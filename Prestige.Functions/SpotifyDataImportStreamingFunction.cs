using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;
using Microsoft.AspNetCore.WebUtilities;
using Newtonsoft.Json;
using System.Security.Cryptography;
using System.Text;
using System.Data.SqlClient;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace Prestige.Functions
{
    public class SpotifyDataImportStreamingFunction
    {
        private readonly ILogger _logger;
        private readonly Container _container;
        private readonly string _sqlConnectionString;
        private readonly BlobServiceClient _blobServiceClient;
        private const int CHUNK_SIZE = 5000; // Process 5000 records at a time
        private const string CONTAINER_NAME = "spotify-imports";

        public SpotifyDataImportStreamingFunction(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<SpotifyDataImportStreamingFunction>();

            string connectionString = Environment.GetEnvironmentVariable("CosmosDBConnectionString");
            string databaseId = "MusicDB";
            string containerId = "RecentlyPlayed";

            var cosmosClient = new CosmosClient(connectionString);
            _container = cosmosClient.GetContainer(databaseId, containerId);
            
            _sqlConnectionString = Environment.GetEnvironmentVariable("SqlConnectionString");
            
            // Initialize blob storage client
            string blobConnectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage");
            _blobServiceClient = new BlobServiceClient(blobConnectionString);
        }

        [Function("SpotifyDataImportStreamingFunction")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req)
        {
            _logger.LogInformation("Streaming import function triggered");

            try
            {
                // Get userId from query parameter
                var userId = req.Query["userId"];
                
                if (string.IsNullOrEmpty(userId))
                {
                    var responseError = req.CreateResponse(HttpStatusCode.BadRequest);
                    await responseError.WriteStringAsync("Missing required query parameter: userId");
                    return responseError;
                }
                
                _logger.LogInformation($"Processing streaming Spotify data import for user: {userId}");

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
                var importStats = new ImportStatistics();
                var fileResults = new List<FileImportResult>();

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
                        _logger.LogInformation($"Processing file: {fileName}");

                        // Upload to blob storage first
                        var blobName = $"{userId}/{batchId}/{fileName}";
                        var blobClient = containerClient.GetBlobClient(blobName);
                        
                        // Calculate hash while uploading
                        string fileHash;
                        using (var hashAlgorithm = SHA256.Create())
                        using (var cryptoStream = new CryptoStream(Stream.Null, hashAlgorithm, CryptoStreamMode.Write))
                        {
                            // Create a forking stream to calculate hash while uploading
                            using (var uploadStream = new MemoryStream())
                            {
                                await section.Body.CopyToAsync(uploadStream);
                                uploadStream.Position = 0;
                                
                                // Calculate hash
                                await uploadStream.CopyToAsync(cryptoStream);
                                cryptoStream.FlushFinalBlock();
                                fileHash = BitConverter.ToString(hashAlgorithm.Hash).Replace("-", "").ToLowerInvariant();
                                
                                // Check if file has been imported before
                                if (await IsFileDuplicateAsync(userId, fileHash))
                                {
                                    _logger.LogWarning($"File {fileName} has already been imported for user {userId}");
                                    fileResults.Add(new FileImportResult 
                                    { 
                                        FileName = fileName, 
                                        Status = "Skipped - Already Imported",
                                        RecordCount = 0
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
                        
                        // Process the file from blob storage in chunks
                        var processResult = await ProcessBlobFileInChunksAsync(blobClient, userId, batchId, importHistoryId, fileName);
                        
                        importStats.TotalItems += processResult.TotalItems;
                        importStats.ImportedItems += processResult.ImportedItems;
                        importStats.SkippedItems += processResult.SkippedItems;
                        
                        fileResults.Add(processResult.FileResult);
                        
                        // Clean up blob after processing
                        await blobClient.DeleteIfExistsAsync();
                        _logger.LogInformation($"Cleaned up blob for file {fileName}");
                    }
                }

                var response = req.CreateResponse(HttpStatusCode.OK);
                var resultMessage = new
                {
                    Summary = $"Streaming import completed. Total items: {importStats.TotalItems}, Imported: {importStats.ImportedItems}, Skipped: {importStats.SkippedItems}",
                    Files = fileResults
                };
                _logger.LogInformation($"Streaming import completed: {JsonConvert.SerializeObject(resultMessage)}");
                await response.WriteAsJsonAsync(resultMessage);
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Exception encountered: {ex.Message}");
                _logger.LogError($"Stack trace: {ex.StackTrace}");

                var responseError = req.CreateResponse(HttpStatusCode.InternalServerError);
                await responseError.WriteStringAsync($"An error occurred while processing your request: {ex.Message}");
                return responseError;
            }
        }
        
        private async Task<ProcessingResult> ProcessBlobFileInChunksAsync(BlobClient blobClient, string userId, string batchId, int importHistoryId, string fileName)
        {
            var result = new ProcessingResult { FileResult = new FileImportResult { FileName = fileName } };
            var documentsToInsert = new List<Document>();
            DateTime? minTimestamp = null;
            DateTime? maxTimestamp = null;
            var dailyListeningTime = new Dictionary<string, int>();
            
            try
            {
                // Download blob as stream
                var downloadResponse = await blobClient.DownloadStreamingAsync();
                using (var stream = downloadResponse.Value.Content)
                using (var reader = new StreamReader(stream))
                using (var jsonReader = new JsonTextReader(reader))
                {
                    var serializer = new JsonSerializer();
                    
                    // Expect array start
                    await jsonReader.ReadAsync();
                    if (jsonReader.TokenType != JsonToken.StartArray)
                    {
                        throw new InvalidOperationException("Expected JSON array");
                    }
                    
                    // Process items in chunks
                    while (await jsonReader.ReadAsync())
                    {
                        if (jsonReader.TokenType == JsonToken.EndArray)
                            break;
                            
                        var item = serializer.Deserialize<SpotifyHistory>(jsonReader);
                        if (item == null)
                            continue;
                            
                        result.TotalItems++;
                        
                        // Skip if it's not a music track
                        if (!string.IsNullOrEmpty(item.SpotifyEpisodeUri) || 
                            !string.IsNullOrEmpty(item.AudiobookUri))
                        {
                            result.SkippedItems++;
                            continue;
                        }
                        
                        // Skip if no track URI
                        if (string.IsNullOrEmpty(item.SpotifyTrackUri))
                        {
                            result.SkippedItems++;
                            continue;
                        }
                        
                        // Validate track duration
                        if (item.MsPlayed <= 0 || item.MsPlayed > 3600000)
                        {
                            result.SkippedItems++;
                            continue;
                        }
                        
                        // Extract track ID
                        var trackId = ExtractTrackIdFromUri(item.SpotifyTrackUri);
                        if (string.IsNullOrEmpty(trackId))
                        {
                            result.SkippedItems++;
                            continue;
                        }
                        
                        // Track timestamps and daily listening time
                        if (DateTime.TryParse(item.Ts, out var timestamp))
                        {
                            if (minTimestamp == null || timestamp < minTimestamp)
                                minTimestamp = timestamp;
                            if (maxTimestamp == null || timestamp > maxTimestamp)
                                maxTimestamp = timestamp;
                                
                            var dateKey = timestamp.Date.ToString("yyyy-MM-dd");
                            if (!dailyListeningTime.ContainsKey(dateKey))
                                dailyListeningTime[dateKey] = 0;
                            dailyListeningTime[dateKey] += item.MsPlayed;
                        }
                        
                        var document = new Document
                        {
                            id = $"{trackId}_{item.Ts}_{batchId}",
                            batchId = batchId,
                            userId = userId,
                            trackId = trackId,
                            duration_ms = item.MsPlayed,
                            played_at = item.Ts
                        };
                        
                        documentsToInsert.Add(document);
                        result.ImportedItems++;
                        
                        // Process chunk when we reach the chunk size
                        if (documentsToInsert.Count >= CHUNK_SIZE)
                        {
                            await ProcessDocumentChunkAsync(documentsToInsert);
                            _logger.LogInformation($"Processed chunk of {documentsToInsert.Count} documents. Total processed: {result.ImportedItems}");
                            documentsToInsert.Clear();
                        }
                    }
                    
                    // Process remaining documents
                    if (documentsToInsert.Count > 0)
                    {
                        await ProcessDocumentChunkAsync(documentsToInsert);
                        _logger.LogInformation($"Processed final chunk of {documentsToInsert.Count} documents");
                    }
                }
                
                // Validate daily listening time
                var suspiciousDays = new List<string>();
                foreach (var day in dailyListeningTime)
                {
                    var hoursListened = day.Value / (1000.0 * 60 * 60);
                    if (hoursListened > 18)
                    {
                        suspiciousDays.Add($"{day.Key}: {hoursListened:F1} hours");
                        _logger.LogWarning($"Suspicious listening time on {day.Key}: {hoursListened:F1} hours");
                    }
                }
                
                // Check for overlapping imports
                var overlaps = await CheckForOverlappingImportsAsync(userId, minTimestamp, maxTimestamp);
                
                // Update import history
                string status = "Completed";
                if (overlaps.Count > 0 || suspiciousDays.Count > 0)
                {
                    var warnings = new List<string>();
                    if (overlaps.Count > 0)
                        warnings.Add($"{overlaps.Count} overlap(s)");
                    if (suspiciousDays.Count > 0)
                        warnings.Add($"{suspiciousDays.Count} suspicious day(s)");
                    status = $"Completed with warnings: {string.Join(" and ", warnings)}";
                }
                
                await UpdateImportHistoryAsync(importHistoryId, result.ImportedItems, minTimestamp, maxTimestamp, status);
                
                result.FileResult.Status = status;
                result.FileResult.RecordCount = result.ImportedItems;
                result.FileResult.Overlaps = overlaps;
                result.FileResult.SuspiciousDays = suspiciousDays;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error processing file {fileName}: {ex.Message}");
                await UpdateImportHistoryAsync(importHistoryId, result.ImportedItems, minTimestamp, maxTimestamp, $"Failed: {ex.Message}");
                result.FileResult.Status = $"Failed: {ex.Message}";
                result.FileResult.RecordCount = result.ImportedItems;
            }
            
            return result;
        }
        
        private async Task ProcessDocumentChunkAsync(List<Document> documents)
        {
            var tasks = documents.Select(doc => _container.UpsertItemAsync(doc, new PartitionKey(doc.userId)));
            await Task.WhenAll(tasks);
        }
        
        private string ExtractTrackIdFromUri(string spotifyUri)
        {
            if (string.IsNullOrEmpty(spotifyUri))
                return null;
                
            var parts = spotifyUri.Split(':');
            if (parts.Length == 3 && parts[0] == "spotify" && parts[1] == "track")
            {
                return parts[2];
            }
            
            return null;
        }
        
        // Reuse existing helper methods from SpotifyDataImportFunction
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
        
        private async Task<List<ImportOverlap>> CheckForOverlappingImportsAsync(string userId, DateTime? minTimestamp, DateTime? maxTimestamp)
        {
            var overlaps = new List<ImportOverlap>();
            
            if (minTimestamp == null || maxTimestamp == null)
                return overlaps;
                
            using (var connection = new SqlConnection(_sqlConnectionString))
            {
                await connection.OpenAsync();
                var command = new SqlCommand(
                    @"SELECT FileName, MinTimestamp, MaxTimestamp, ImportDate 
                      FROM ImportHistories 
                      WHERE UserId = @userId 
                      AND Status LIKE 'Completed%'
                      AND MinTimestamp IS NOT NULL 
                      AND MaxTimestamp IS NOT NULL
                      AND (
                        (MinTimestamp <= @maxTs AND MaxTimestamp >= @minTs) OR
                        (MinTimestamp >= @minTs AND MinTimestamp <= @maxTs) OR
                        (MaxTimestamp >= @minTs AND MaxTimestamp <= @maxTs)
                      )",
                    connection);
                command.Parameters.AddWithValue("@userId", userId);
                command.Parameters.AddWithValue("@minTs", minTimestamp);
                command.Parameters.AddWithValue("@maxTs", maxTimestamp);
                
                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        overlaps.Add(new ImportOverlap
                        {
                            FileName = reader.GetString(0),
                            MinTimestamp = reader.GetDateTime(1),
                            MaxTimestamp = reader.GetDateTime(2),
                            ImportDate = reader.GetDateTime(3)
                        });
                    }
                }
            }
            
            return overlaps;
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
                command.Parameters.AddWithValue("@status", "Processing");
                
                return (int)await command.ExecuteScalarAsync();
            }
        }
        
        private async Task UpdateImportHistoryAsync(int importHistoryId, int recordCount, DateTime? minTimestamp, DateTime? maxTimestamp, string status)
        {
            using (var connection = new SqlConnection(_sqlConnectionString))
            {
                await connection.OpenAsync();
                var command = new SqlCommand(
                    @"UPDATE ImportHistories 
                      SET RecordCount = @recordCount, MinTimestamp = @minTimestamp, MaxTimestamp = @maxTimestamp, Status = @status
                      WHERE Id = @id",
                    connection);
                command.Parameters.AddWithValue("@id", importHistoryId);
                command.Parameters.AddWithValue("@recordCount", recordCount);
                command.Parameters.AddWithValue("@minTimestamp", (object)minTimestamp ?? DBNull.Value);
                command.Parameters.AddWithValue("@maxTimestamp", (object)maxTimestamp ?? DBNull.Value);
                command.Parameters.AddWithValue("@status", status);
                
                await command.ExecuteNonQueryAsync();
            }
        }
        
        // Helper classes
        public class ProcessingResult
        {
            public int TotalItems { get; set; }
            public int ImportedItems { get; set; }
            public int SkippedItems { get; set; }
            public FileImportResult FileResult { get; set; }
        }
        
        public class ImportStatistics
        {
            public int TotalItems { get; set; }
            public int ImportedItems { get; set; }
            public int SkippedItems { get; set; }
        }
        
        public class FileImportResult
        {
            public string FileName { get; set; }
            public string Status { get; set; }
            public int RecordCount { get; set; }
            public List<ImportOverlap> Overlaps { get; set; }
            public List<string> SuspiciousDays { get; set; }
        }
        
        public class ImportOverlap
        {
            public string FileName { get; set; }
            public DateTime MinTimestamp { get; set; }
            public DateTime MaxTimestamp { get; set; }
            public DateTime ImportDate { get; set; }
        }

        public class SpotifyHistory
        {
            [JsonProperty("ts")]
            public string Ts { get; set; }
            
            [JsonProperty("platform")]
            public string Platform { get; set; }
            
            [JsonProperty("ms_played")]
            public int MsPlayed { get; set; }
            
            [JsonProperty("conn_country")]
            public string ConnCountry { get; set; }
            
            [JsonProperty("ip_addr")]
            public string IpAddr { get; set; }
            
            [JsonProperty("master_metadata_track_name")]
            public string MasterMetadataTrackName { get; set; }
            
            [JsonProperty("master_metadata_album_artist_name")]
            public string MasterMetadataAlbumArtistName { get; set; }
            
            [JsonProperty("master_metadata_album_album_name")]
            public string MasterMetadataAlbumAlbumName { get; set; }
            
            [JsonProperty("spotify_track_uri")]
            public string SpotifyTrackUri { get; set; }
            
            [JsonProperty("episode_name")]
            public string EpisodeName { get; set; }
            
            [JsonProperty("episode_show_name")]
            public string EpisodeShowName { get; set; }
            
            [JsonProperty("spotify_episode_uri")]
            public string SpotifyEpisodeUri { get; set; }
            
            [JsonProperty("audiobook_title")]
            public string AudiobookTitle { get; set; }
            
            [JsonProperty("audiobook_uri")]
            public string AudiobookUri { get; set; }
            
            [JsonProperty("audiobook_chapter_uri")]
            public string AudiobookChapterUri { get; set; }
            
            [JsonProperty("audiobook_chapter_title")]
            public string AudiobookChapterTitle { get; set; }
            
            [JsonProperty("reason_start")]
            public string ReasonStart { get; set; }
            
            [JsonProperty("reason_end")]
            public string ReasonEnd { get; set; }
            
            [JsonProperty("shuffle")]
            public bool Shuffle { get; set; }
            
            [JsonProperty("skipped")]
            public bool? Skipped { get; set; }
            
            [JsonProperty("offline")]
            public bool Offline { get; set; }
            
            [JsonProperty("offline_timestamp")]
            public long OfflineTimestamp { get; set; }
            
            [JsonProperty("incognito_mode")]
            public bool IncognitoMode { get; set; }
        }

        public class Document
        {
            public string id { get; set; }
            public string batchId { get; set; }
            public string userId { get; set; }
            public string trackId { get; set; }
            public int duration_ms { get; set; }
            public string played_at { get; set; }
        }
    }
}