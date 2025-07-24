using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;
using Microsoft.AspNetCore.WebUtilities;
using Newtonsoft.Json;
using System.Security.Cryptography;
using System.Data.SqlClient;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Messaging.ServiceBus;

namespace Prestige.Functions
{
    public class SpotifyDataImportOrchestrator
    {
        private readonly ILogger _logger;
        private readonly string _sqlConnectionString;
        private readonly BlobServiceClient _blobServiceClient;
        private readonly ServiceBusClient _serviceBusClient;
        private const string CONTAINER_NAME = "spotify-imports";
        private const string QUEUE_NAME = "spotify-import-chunks";

        public SpotifyDataImportOrchestrator(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<SpotifyDataImportOrchestrator>();
            _sqlConnectionString = Environment.GetEnvironmentVariable("SqlConnectionString");
            
            // Initialize blob storage client
            string blobConnectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage");
            _blobServiceClient = new BlobServiceClient(blobConnectionString);
            
            // Initialize Service Bus client
            string serviceBusConnectionString = Environment.GetEnvironmentVariable("ServiceBusConnectionString");
            _serviceBusClient = new ServiceBusClient(serviceBusConnectionString);
        }

        [Function("SpotifyDataImportOrchestrator")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestData req)
        {
            _logger.LogInformation("Import orchestrator triggered");

            try
            {
                // Get userId from query parameter
                var userId = req.Query["userId"];
                
                if (string.IsNullOrEmpty(userId))
                {
                    var responseError = req.CreateResponse(HttpStatusCode.BadRequest);
                    responseError.Headers.Add("Access-Control-Allow-Origin", "*");
                    await responseError.WriteStringAsync("Missing required query parameter: userId");
                    return responseError;
                }
                
                _logger.LogInformation($"Processing import orchestration for user: {userId}");

                // Check Content-Type header
                if (!req.Headers.TryGetValues("Content-Type", out var contentTypeValues))
                {
                    var responseError = req.CreateResponse(HttpStatusCode.BadRequest);
                    await responseError.WriteStringAsync("Missing Content-Type header.");
                    return responseError;
                }

                var contentType = string.Join("; ", contentTypeValues);
                var mediaTypeHeader = MediaTypeHeaderValue.Parse(contentType);

                if (!mediaTypeHeader.MediaType.Equals("multipart/form-data", StringComparison.OrdinalIgnoreCase))
                {
                    var responseError = req.CreateResponse(HttpStatusCode.BadRequest);
                    await responseError.WriteStringAsync("Invalid Content-Type header. Expected 'multipart/form-data'.");
                    return responseError;
                }

                // Get boundary
                var boundary = HeaderUtilities.RemoveQuotes(mediaTypeHeader.Boundary).Value;
                if (string.IsNullOrEmpty(boundary))
                {
                    var responseError = req.CreateResponse(HttpStatusCode.BadRequest);
                    await responseError.WriteStringAsync("Could not determine boundary from Content-Type header.");
                    return responseError;
                }

                // Create blob container if it doesn't exist
                var containerClient = _blobServiceClient.GetBlobContainerClient(CONTAINER_NAME);
                await containerClient.CreateIfNotExistsAsync(PublicAccessType.None);

                // Create a single batchId for this import session
                var batchId = Guid.NewGuid().ToString();
                var fileResults = new List<FileUploadResult>();

                // Create Service Bus sender
                await using var sender = _serviceBusClient.CreateSender(QUEUE_NAME);

                // Read the multipart form data
                var reader = new MultipartReader(boundary, req.Body);
                MultipartSection section;
                
                while ((section = await reader.ReadNextSectionAsync()) != null)
                {
                    var hasContentDispositionHeader = ContentDispositionHeaderValue.TryParse(section.ContentDisposition, out var contentDisposition);
                    if (!hasContentDispositionHeader)
                        continue;

                    if (contentDisposition.DispositionType.Equals("form-data") && contentDisposition.FileName.HasValue)
                    {
                        var fileName = contentDisposition.FileName.Value;
                        _logger.LogInformation($"Processing file upload: {fileName}");

                        // Upload to blob storage
                        var blobName = $"{userId}/{batchId}/{fileName}";
                        var blobClient = containerClient.GetBlobClient(blobName);
                        
                        // Calculate hash while uploading
                        string fileHash;
                        long fileSize;
                        using (var hashAlgorithm = SHA256.Create())
                        using (var cryptoStream = new CryptoStream(Stream.Null, hashAlgorithm, CryptoStreamMode.Write))
                        {
                            using (var uploadStream = new MemoryStream())
                            {
                                await section.Body.CopyToAsync(uploadStream);
                                fileSize = uploadStream.Length;
                                uploadStream.Position = 0;
                                
                                // Calculate hash
                                await uploadStream.CopyToAsync(cryptoStream);
                                cryptoStream.FlushFinalBlock();
                                fileHash = BitConverter.ToString(hashAlgorithm.Hash).Replace("-", "").ToLowerInvariant();
                                
                                // Check if file has been imported before
                                if (await IsFileDuplicateAsync(userId, fileHash))
                                {
                                    _logger.LogWarning($"File {fileName} has already been imported for user {userId}");
                                    fileResults.Add(new FileUploadResult 
                                    { 
                                        FileName = fileName, 
                                        Status = "Skipped - Already Imported",
                                        ImportHistoryId = 0
                                    });
                                    continue;
                                }
                                
                                // Upload to blob storage
                                uploadStream.Position = 0;
                                await blobClient.UploadAsync(uploadStream, true);
                                _logger.LogInformation($"File {fileName} uploaded to blob storage");
                            }
                        }
                        
                        // Create import history record
                        var importHistoryId = await CreateImportHistoryAsync(userId, fileHash, fileName, batchId);
                        
                        // Queue processing job
                        var processingJob = new ImportProcessingJob
                        {
                            ImportHistoryId = importHistoryId,
                            UserId = userId,
                            BatchId = batchId,
                            BlobName = blobName,
                            FileName = fileName,
                            FileSize = fileSize,
                            ChunkSize = 5000 // Process 5000 records at a time
                        };
                        
                        var message = new ServiceBusMessage(JsonConvert.SerializeObject(processingJob))
                        {
                            Subject = "spotify-import",
                            SessionId = userId, // Use userId as session to process user's files in order
                            MessageId = $"{batchId}_{fileName}"
                        };
                        
                        await sender.SendMessageAsync(message);
                        _logger.LogInformation($"Queued processing job for {fileName}");
                        
                        fileResults.Add(new FileUploadResult 
                        { 
                            FileName = fileName, 
                            Status = "Queued for Processing",
                            ImportHistoryId = importHistoryId
                        });
                    }
                }

                var response = req.CreateResponse(HttpStatusCode.Accepted);
                response.Headers.Add("Access-Control-Allow-Origin", "*");
                response.Headers.Add("Access-Control-Allow-Methods", "GET, POST, OPTIONS");
                response.Headers.Add("Access-Control-Allow-Headers", "Content-Type");
                var resultMessage = new
                {
                    BatchId = batchId,
                    Message = "Files uploaded and queued for processing",
                    Files = fileResults,
                    StatusUrl = $"/api/import-status/{batchId}"
                };
                await response.WriteAsJsonAsync(resultMessage);
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Exception encountered: {ex.Message}");
                _logger.LogError($"Stack trace: {ex.StackTrace}");

                var responseError = req.CreateResponse(HttpStatusCode.InternalServerError);
                responseError.Headers.Add("Access-Control-Allow-Origin", "*");
                await responseError.WriteStringAsync($"An error occurred while processing your request: {ex.Message}");
                return responseError;
            }
        }
        
        private async Task<bool> IsFileDuplicateAsync(string userId, string fileHash)
        {
            using (var connection = new SqlConnection(_sqlConnectionString))
            {
                await connection.OpenAsync();
                var command = new SqlCommand(
                    "SELECT COUNT(*) FROM ImportHistories WHERE UserId = @userId AND FileHash = @fileHash AND Status LIKE 'Completed%'",
                    connection);
                command.Parameters.AddWithValue("@userId", userId);
                command.Parameters.AddWithValue("@fileHash", fileHash);
                
                var count = (int)await command.ExecuteScalarAsync();
                return count > 0;
            }
        }
        
        private async Task<int> CreateImportHistoryAsync(string userId, string fileHash, string fileName, string batchId)
        {
            using (var connection = new SqlConnection(_sqlConnectionString))
            {
                await connection.OpenAsync();
                var command = new SqlCommand(
                    @"INSERT INTO ImportHistories (UserId, FileHash, FileName, BatchId, ImportDate, Status, RecordCount) 
                      VALUES (@userId, @fileHash, @fileName, @batchId, @importDate, @status, 0);
                      SELECT CAST(SCOPE_IDENTITY() as int);",
                    connection);
                command.Parameters.AddWithValue("@userId", userId);
                command.Parameters.AddWithValue("@fileHash", fileHash);
                command.Parameters.AddWithValue("@fileName", fileName);
                command.Parameters.AddWithValue("@batchId", batchId);
                command.Parameters.AddWithValue("@importDate", DateTime.UtcNow);
                command.Parameters.AddWithValue("@status", "Queued");
                
                return (int)await command.ExecuteScalarAsync();
            }
        }
        
        // Helper classes
        public class FileUploadResult
        {
            public string FileName { get; set; }
            public string Status { get; set; }
            public int ImportHistoryId { get; set; }
        }
        
        public class ImportProcessingJob
        {
            public int ImportHistoryId { get; set; }
            public string UserId { get; set; }
            public string BatchId { get; set; }
            public string BlobName { get; set; }
            public string FileName { get; set; }
            public long FileSize { get; set; }
            public int ChunkSize { get; set; }
        }
    }
}