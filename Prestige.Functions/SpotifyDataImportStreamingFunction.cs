using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;
using Newtonsoft.Json;
using Prestige.Functions.Helpers;

namespace Prestige.Functions
{
    public class SpotifyDataImportStreamingFunction
    {
        private readonly ILogger _logger;
        private readonly Container _container;
        private readonly string _sqlConnectionString;
        private readonly BlobServiceClient _blobServiceClient;
        private readonly CosmosBulkImporter _bulkImporter;
        private const int CHUNK_SIZE = 10000;
        private const string CONTAINER_NAME = "spotify-imports";

        public SpotifyDataImportStreamingFunction(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<SpotifyDataImportStreamingFunction>();

            var connectionString = Environment.GetEnvironmentVariable("CosmosDBConnectionString");
            const string databaseId = "MusicDB";
            const string containerId = "RecentlyPlayed";

            var cosmosClient = new CosmosClient(connectionString);
            _container = cosmosClient.GetContainer(databaseId, containerId);

            _sqlConnectionString = Environment.GetEnvironmentVariable("SqlConnectionString");

            var blobConnectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage");
            _blobServiceClient = new BlobServiceClient(blobConnectionString);

            _bulkImporter = new CosmosBulkImporter(connectionString, databaseId, containerId, _logger);
        }

        [Function("SpotifyDataImportStreamingFunction")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", "options")] HttpRequestData req)
        {
            _logger.LogInformation("Streaming import function triggered with method: {Method}", req.Method);

            if (req.Method.Equals("OPTIONS", StringComparison.OrdinalIgnoreCase))
            {
                var corsResponse = req.CreateResponse(HttpStatusCode.OK);
                AddCorsHeaders(corsResponse);
                return corsResponse;
            }

            try
            {
                var userId = req.Query["userId"];
                var requestedSessionId = req.Query["sessionId"];

                if (string.IsNullOrWhiteSpace(userId))
                {
                    var responseError = req.CreateResponse(HttpStatusCode.BadRequest);
                    AddCorsHeaders(responseError);
                    await responseError.WriteStringAsync("Missing required query parameter: userId");
                    return responseError;
                }

                if (!req.Headers.TryGetValues("Content-Type", out var contentTypeValues))
                {
                    var responseError = req.CreateResponse(HttpStatusCode.BadRequest);
                    AddCorsHeaders(responseError);
                    await responseError.WriteStringAsync("Missing Content-Type header.");
                    return responseError;
                }

                var contentType = string.Join("; ", contentTypeValues);
                var mediaTypeHeader = MediaTypeHeaderValue.Parse(contentType);

                if (!mediaTypeHeader.MediaType.Equals("multipart/form-data", StringComparison.OrdinalIgnoreCase))
                {
                    var responseError = req.CreateResponse(HttpStatusCode.BadRequest);
                    AddCorsHeaders(responseError);
                    await responseError.WriteStringAsync("Invalid Content-Type header. Expected 'multipart/form-data'.");
                    return responseError;
                }

                var boundary = HeaderUtilities.RemoveQuotes(mediaTypeHeader.Boundary).Value;
                if (string.IsNullOrWhiteSpace(boundary))
                {
                    var responseError = req.CreateResponse(HttpStatusCode.BadRequest);
                    AddCorsHeaders(responseError);
                    await responseError.WriteStringAsync("Could not determine boundary from Content-Type header.");
                    return responseError;
                }

                var containerClient = _blobServiceClient.GetBlobContainerClient(CONTAINER_NAME);
                await containerClient.CreateIfNotExistsAsync(PublicAccessType.None);

                var batchId = !string.IsNullOrWhiteSpace(requestedSessionId)
                    ? requestedSessionId
                    : Guid.NewGuid().ToString();

                var importStats = new ImportStatistics();
                var fileResults = new List<FileImportResult>();

                var reader = new MultipartReader(boundary, req.Body);
                MultipartSection section;

                while ((section = await reader.ReadNextSectionAsync()) != null)
                {
                    var hasContentDispositionHeader = ContentDispositionHeaderValue.TryParse(section.ContentDisposition, out var contentDisposition);
                    if (!hasContentDispositionHeader || contentDisposition == null)
                    {
                        continue;
                    }

                    if (!contentDisposition.DispositionType.Equals("form-data") || !contentDisposition.FileName.HasValue)
                    {
                        continue;
                    }

                    var fileName = NormalizeUploadedFileName(contentDisposition.FileName.Value);
                    _logger.LogInformation("Processing file {FileName} for user {UserId} in batch {BatchId}", fileName, userId, batchId);

                    var tempFilePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}-{fileName}");
                    try
                    {
                        await using (var tempFileStream = File.Create(tempFilePath))
                        {
                            await section.Body.CopyToAsync(tempFileStream);
                        }

                        var fileHash = await ComputeSha256Async(tempFilePath);
                        if (await IsFileDuplicateAsync(userId, fileHash))
                        {
                            _logger.LogWarning("File {FileName} has already been imported for user {UserId}", fileName, userId);
                            fileResults.Add(new FileImportResult
                            {
                                FileName = fileName,
                                Status = "Skipped - Already Imported",
                                RecordCount = 0,
                                Overlaps = new List<ImportOverlap>(),
                                SuspiciousDays = new List<string>()
                            });
                            continue;
                        }

                        var blobName = $"{userId}/{batchId}/{fileName}";
                        var blobClient = containerClient.GetBlobClient(blobName);

                        await using (var uploadStream = File.OpenRead(tempFilePath))
                        {
                            await blobClient.UploadAsync(uploadStream, overwrite: true);
                        }

                        var importHistoryId = await CreateImportHistoryAsync(userId, fileHash, fileName, batchId);

                        var processResult = IsZipFile(fileName)
                            ? await ProcessZipFileInChunksAsync(tempFilePath, userId, batchId, importHistoryId, fileName)
                            : await ProcessBlobFileInChunksAsync(blobClient, userId, batchId, importHistoryId, fileName);

                        importStats.TotalItems += processResult.TotalItems;
                        importStats.ImportedItems += processResult.ImportedItems;
                        importStats.SkippedItems += processResult.SkippedItems;

                        fileResults.Add(processResult.FileResult);

                        await blobClient.DeleteIfExistsAsync();
                    }
                    finally
                    {
                        TryDelete(tempFilePath);
                    }
                }

                var response = req.CreateResponse(HttpStatusCode.OK);
                AddCorsHeaders(response);

                var resultMessage = new
                {
                    BatchId = batchId,
                    Summary = $"Streaming import completed. Total items: {importStats.TotalItems}, Imported: {importStats.ImportedItems}, Skipped: {importStats.SkippedItems}",
                    TotalItems = importStats.TotalItems,
                    ImportedItems = importStats.ImportedItems,
                    SkippedItems = importStats.SkippedItems,
                    Files = fileResults
                };

                _logger.LogInformation("Streaming import completed: {Result}", JsonConvert.SerializeObject(resultMessage));
                await response.WriteAsJsonAsync(resultMessage);
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while processing the streaming import request.");

                var responseError = req.CreateResponse(HttpStatusCode.InternalServerError);
                AddCorsHeaders(responseError);
                await responseError.WriteStringAsync($"An error occurred while processing your request: {ex.Message}");
                return responseError;
            }
        }

        private async Task<ProcessingResult> ProcessBlobFileInChunksAsync(
            BlobClient blobClient,
            string userId,
            string batchId,
            int importHistoryId,
            string fileName)
        {
            var buffer = new ImportBuffer();

            try
            {
                var downloadResponse = await blobClient.DownloadStreamingAsync();
                await using (var stream = downloadResponse.Value.Content)
                {
                    await ProcessJsonStreamInChunksAsync(stream, userId, batchId, buffer);
                }

                await FlushPendingDocumentsAsync(buffer.DocumentsToInsert);
                return await FinalizeProcessingAsync(userId, importHistoryId, fileName, buffer);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing JSON file {FileName}", fileName);
                await UpdateImportHistoryAsync(importHistoryId, buffer.ImportedItems, buffer.MinTimestamp, buffer.MaxTimestamp, $"Failed: {ex.Message}");

                return new ProcessingResult
                {
                    TotalItems = buffer.TotalItems,
                    ImportedItems = buffer.ImportedItems,
                    SkippedItems = buffer.SkippedItems,
                    FileResult = new FileImportResult
                    {
                        FileName = fileName,
                        Status = $"Failed: {ex.Message}",
                        RecordCount = buffer.ImportedItems,
                        Overlaps = new List<ImportOverlap>(),
                        SuspiciousDays = new List<string>()
                    }
                };
            }
        }

        private async Task<ProcessingResult> ProcessZipFileInChunksAsync(
            string zipFilePath,
            string userId,
            string batchId,
            int importHistoryId,
            string fileName)
        {
            var buffer = new ImportBuffer();

            try
            {
                using var archive = ZipFile.OpenRead(zipFilePath);
                var jsonEntries = archive.Entries
                    .Where(entry => !string.IsNullOrWhiteSpace(entry.Name) && entry.Name.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                    .ToList();

                if (!jsonEntries.Any())
                {
                    throw new InvalidOperationException("The uploaded zip file did not contain any Spotify streaming history JSON files.");
                }

                foreach (var entry in jsonEntries)
                {
                    await using var entryStream = entry.Open();
                    await ProcessJsonStreamInChunksAsync(entryStream, userId, batchId, buffer);
                }

                await FlushPendingDocumentsAsync(buffer.DocumentsToInsert);
                return await FinalizeProcessingAsync(userId, importHistoryId, fileName, buffer);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing zip file {FileName}", fileName);
                await UpdateImportHistoryAsync(importHistoryId, buffer.ImportedItems, buffer.MinTimestamp, buffer.MaxTimestamp, $"Failed: {ex.Message}");

                return new ProcessingResult
                {
                    TotalItems = buffer.TotalItems,
                    ImportedItems = buffer.ImportedItems,
                    SkippedItems = buffer.SkippedItems,
                    FileResult = new FileImportResult
                    {
                        FileName = fileName,
                        Status = $"Failed: {ex.Message}",
                        RecordCount = buffer.ImportedItems,
                        Overlaps = new List<ImportOverlap>(),
                        SuspiciousDays = new List<string>()
                    }
                };
            }
        }

        private async Task ProcessJsonStreamInChunksAsync(Stream stream, string userId, string batchId, ImportBuffer buffer)
        {
            using var reader = new StreamReader(stream);
            using var jsonReader = new JsonTextReader(reader);
            var serializer = new JsonSerializer();

            await jsonReader.ReadAsync();
            if (jsonReader.TokenType != JsonToken.StartArray)
            {
                throw new InvalidOperationException("Expected JSON array");
            }

            while (await jsonReader.ReadAsync())
            {
                if (jsonReader.TokenType == JsonToken.EndArray)
                {
                    break;
                }

                var item = serializer.Deserialize<SpotifyHistory>(jsonReader);
                if (item == null)
                {
                    continue;
                }

                buffer.TotalItems++;

                if (!string.IsNullOrEmpty(item.SpotifyEpisodeUri) || !string.IsNullOrEmpty(item.AudiobookUri))
                {
                    buffer.SkippedItems++;
                    continue;
                }

                if (string.IsNullOrEmpty(item.SpotifyTrackUri))
                {
                    buffer.SkippedItems++;
                    continue;
                }

                if (item.MsPlayed <= 0 || item.MsPlayed > 3600000)
                {
                    buffer.SkippedItems++;
                    continue;
                }

                var trackId = ExtractTrackIdFromUri(item.SpotifyTrackUri);
                if (string.IsNullOrEmpty(trackId))
                {
                    buffer.SkippedItems++;
                    continue;
                }

                if (DateTime.TryParse(item.Ts, out var timestamp))
                {
                    if (buffer.MinTimestamp == null || timestamp < buffer.MinTimestamp)
                    {
                        buffer.MinTimestamp = timestamp;
                    }

                    if (buffer.MaxTimestamp == null || timestamp > buffer.MaxTimestamp)
                    {
                        buffer.MaxTimestamp = timestamp;
                    }

                    var dateKey = timestamp.Date.ToString("yyyy-MM-dd");
                    if (!buffer.DailyListeningTime.ContainsKey(dateKey))
                    {
                        buffer.DailyListeningTime[dateKey] = 0;
                    }

                    buffer.DailyListeningTime[dateKey] += item.MsPlayed;
                }

                buffer.DocumentsToInsert.Add(new Document
                {
                    id = $"{trackId}_{item.Ts}_{batchId}",
                    batchId = batchId,
                    userId = userId,
                    trackId = trackId,
                    duration_ms = item.MsPlayed,
                    played_at = item.Ts
                });

                buffer.ImportedItems++;

                if (buffer.DocumentsToInsert.Count >= CHUNK_SIZE)
                {
                    await FlushPendingDocumentsAsync(buffer.DocumentsToInsert);
                }
            }
        }

        private async Task FlushPendingDocumentsAsync(List<Document> documentsToInsert)
        {
            if (documentsToInsert.Count == 0)
            {
                return;
            }

            var bulkResult = await _bulkImporter.BulkImportAsync(documentsToInsert, doc => doc.userId);
            _logger.LogInformation(
                "Processed chunk of {Count} documents. Success: {SuccessCount}, Failed: {FailureCount}, RUs: {RequestCharge:F2}",
                documentsToInsert.Count,
                bulkResult.SuccessCount,
                bulkResult.FailureCount,
                bulkResult.TotalRequestCharge);

            documentsToInsert.Clear();
        }

        private async Task<ProcessingResult> FinalizeProcessingAsync(
            string userId,
            int importHistoryId,
            string fileName,
            ImportBuffer buffer)
        {
            var suspiciousDays = new List<string>();
            foreach (var day in buffer.DailyListeningTime)
            {
                var hoursListened = day.Value / (1000.0 * 60 * 60);
                if (hoursListened > 18)
                {
                    suspiciousDays.Add($"{day.Key}: {hoursListened:F1} hours");
                    _logger.LogWarning("Suspicious listening time on {Date}: {Hours:F1} hours", day.Key, hoursListened);
                }
            }

            var overlaps = await CheckForOverlappingImportsAsync(userId, buffer.MinTimestamp, buffer.MaxTimestamp);

            var status = "Completed";
            if (overlaps.Count > 0 || suspiciousDays.Count > 0)
            {
                var warnings = new List<string>();
                if (overlaps.Count > 0)
                {
                    warnings.Add($"{overlaps.Count} overlap(s)");
                }

                if (suspiciousDays.Count > 0)
                {
                    warnings.Add($"{suspiciousDays.Count} suspicious day(s)");
                }

                status = $"Completed with warnings: {string.Join(" and ", warnings)}";
            }

            await UpdateImportHistoryAsync(importHistoryId, buffer.ImportedItems, buffer.MinTimestamp, buffer.MaxTimestamp, status);

            return new ProcessingResult
            {
                TotalItems = buffer.TotalItems,
                ImportedItems = buffer.ImportedItems,
                SkippedItems = buffer.SkippedItems,
                FileResult = new FileImportResult
                {
                    FileName = fileName,
                    Status = status,
                    RecordCount = buffer.ImportedItems,
                    Overlaps = overlaps,
                    SuspiciousDays = suspiciousDays
                }
            };
        }

        private static void AddCorsHeaders(HttpResponseData response)
        {
            response.Headers.Add("Access-Control-Allow-Origin", "*");
            response.Headers.Add("Access-Control-Allow-Methods", "GET, POST, OPTIONS");
            response.Headers.Add("Access-Control-Allow-Headers", "Content-Type, Authorization, x-functions-key");
            response.Headers.Add("Access-Control-Max-Age", "3600");
        }

        private static string NormalizeUploadedFileName(string fileName)
        {
            return Path.GetFileName(fileName.Trim('"'));
        }

        private static bool IsZipFile(string fileName)
        {
            return fileName.EndsWith(".zip", StringComparison.OrdinalIgnoreCase);
        }

        private static void TryDelete(string path)
        {
            try
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
            catch
            {
            }
        }

        private async Task<string> ComputeSha256Async(string filePath)
        {
            using var sha256 = SHA256.Create();
            await using var fileStream = File.OpenRead(filePath);
            var hash = await sha256.ComputeHashAsync(fileStream);
            return Convert.ToHexString(hash).ToLowerInvariant();
        }

        private static string ExtractTrackIdFromUri(string spotifyUri)
        {
            if (string.IsNullOrEmpty(spotifyUri))
            {
                return null;
            }

            var parts = spotifyUri.Split(':');
            if (parts.Length == 3 && parts[0] == "spotify" && parts[1] == "track")
            {
                return parts[2];
            }

            return null;
        }

        private async Task<bool> IsFileDuplicateAsync(string userId, string fileHash)
        {
            using var connection = new SqlConnection(_sqlConnectionString);
            await connection.OpenAsync();

            var command = new SqlCommand(
                "SELECT COUNT(*) FROM ImportHistories WHERE UserId = @userId AND FileHash = @fileHash AND Status LIKE 'Completed%'",
                connection);
            command.Parameters.AddWithValue("@userId", userId);
            command.Parameters.AddWithValue("@fileHash", fileHash);

            var count = (int)await command.ExecuteScalarAsync();
            return count > 0;
        }

        private async Task<List<ImportOverlap>> CheckForOverlappingImportsAsync(string userId, DateTime? minTimestamp, DateTime? maxTimestamp)
        {
            var overlaps = new List<ImportOverlap>();

            if (minTimestamp == null || maxTimestamp == null)
            {
                return overlaps;
            }

            using var connection = new SqlConnection(_sqlConnectionString);
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

            using var reader = await command.ExecuteReaderAsync();
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

            return overlaps;
        }

        private async Task<int> CreateImportHistoryAsync(string userId, string fileHash, string fileName, string batchId)
        {
            using var connection = new SqlConnection(_sqlConnectionString);
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

        private async Task UpdateImportHistoryAsync(int importHistoryId, int recordCount, DateTime? minTimestamp, DateTime? maxTimestamp, string status)
        {
            using var connection = new SqlConnection(_sqlConnectionString);
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

        private sealed class ImportBuffer
        {
            public int TotalItems { get; set; }
            public int ImportedItems { get; set; }
            public int SkippedItems { get; set; }
            public List<Document> DocumentsToInsert { get; } = new();
            public DateTime? MinTimestamp { get; set; }
            public DateTime? MaxTimestamp { get; set; }
            public Dictionary<string, int> DailyListeningTime { get; } = new();
        }

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
            public List<ImportOverlap> Overlaps { get; set; } = new();
            public List<string> SuspiciousDays { get; set; } = new();
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
            public long? OfflineTimestamp { get; set; }

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
