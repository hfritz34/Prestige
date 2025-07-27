using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Data.SqlClient;
using Azure.Storage.Blobs;

namespace Prestige.Functions
{
    public class SpotifyDataImportProcessor
    {
        private readonly ILogger _logger;
        private readonly Container _container;
        private readonly string _sqlConnectionString;
        private readonly BlobServiceClient _blobServiceClient;
        private const string CONTAINER_NAME = "spotify-imports";

        public SpotifyDataImportProcessor(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<SpotifyDataImportProcessor>();

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

        [Function("SpotifyDataImportProcessor")]
        public async Task Run(
            [ServiceBusTrigger("spotify-import-chunks", Connection = "ServiceBusConnectionString", IsSessionsEnabled = true)] 
            string message,
            FunctionContext context)
        {
            _logger.LogInformation($"Processing import job: {message}");

            try
            {
                var job = JsonConvert.DeserializeObject<ImportProcessingJob>(message);
                
                // Update status to Processing
                await UpdateImportStatusAsync(job.ImportHistoryId, "Processing");
                
                // Get blob client
                var containerClient = _blobServiceClient.GetBlobContainerClient(CONTAINER_NAME);
                var blobClient = containerClient.GetBlobClient(job.BlobName);
                
                if (!await blobClient.ExistsAsync())
                {
                    _logger.LogError($"Blob not found: {job.BlobName}");
                    await UpdateImportStatusAsync(job.ImportHistoryId, "Failed: File not found");
                    return;
                }
                
                // Process the file
                var result = await ProcessBlobFileAsync(blobClient, job);
                
                // Update import history with results
                await UpdateImportHistoryAsync(
                    job.ImportHistoryId, 
                    result.ImportedItems, 
                    result.MinTimestamp, 
                    result.MaxTimestamp, 
                    result.Status);
                
                // Clean up blob after successful processing
                if (result.Status.StartsWith("Completed"))
                {
                    await blobClient.DeleteIfExistsAsync();
                    _logger.LogInformation($"Cleaned up blob: {job.BlobName}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error processing import job: {ex.Message}");
                _logger.LogError($"Stack trace: {ex.StackTrace}");
                throw; // Let Service Bus handle retry
            }
        }
        
        private async Task<ProcessingResult> ProcessBlobFileAsync(BlobClient blobClient, ImportProcessingJob job)
        {
            var result = new ProcessingResult();
            var documentsToInsert = new List<Document>();
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
                    
                    // Process items
                    while (await jsonReader.ReadAsync())
                    {
                        if (jsonReader.TokenType == JsonToken.EndArray)
                            break;
                            
                        var item = serializer.Deserialize<SpotifyHistory>(jsonReader);
                        if (item == null)
                            continue;
                            
                        result.TotalItems++;
                        
                        // Apply all validation rules
                        if (!IsValidTrack(item, out var trackId))
                        {
                            result.SkippedItems++;
                            continue;
                        }
                        
                        // Track timestamps and daily listening time
                        if (DateTime.TryParse(item.Ts, out var timestamp))
                        {
                            if (result.MinTimestamp == null || timestamp < result.MinTimestamp)
                                result.MinTimestamp = timestamp;
                            if (result.MaxTimestamp == null || timestamp > result.MaxTimestamp)
                                result.MaxTimestamp = timestamp;
                                
                            var dateKey = timestamp.Date.ToString("yyyy-MM-dd");
                            if (!dailyListeningTime.ContainsKey(dateKey))
                                dailyListeningTime[dateKey] = 0;
                            dailyListeningTime[dateKey] += item.MsPlayed;
                        }
                        
                        var document = new Document
                        {
                            id = $"{trackId}_{item.Ts}_{job.BatchId}",
                            batchId = job.BatchId,
                            userId = job.UserId,
                            trackId = trackId,
                            duration_ms = item.MsPlayed,
                            played_at = item.Ts
                        };
                        
                        documentsToInsert.Add(document);
                        result.ImportedItems++;
                        
                        // Process chunk when we reach the chunk size
                        if (documentsToInsert.Count >= job.ChunkSize)
                        {
                            await ProcessDocumentChunkAsync(documentsToInsert);
                            _logger.LogInformation($"Processed chunk of {documentsToInsert.Count} documents. Total: {result.ImportedItems}");
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
                
                // Validate and generate warnings
                var warnings = new List<string>();
                
                // Check daily listening time
                var suspiciousDays = dailyListeningTime
                    .Where(d => d.Value / (1000.0 * 60 * 60) > 18)
                    .Select(d => $"{d.Key}: {d.Value / (1000.0 * 60 * 60):F1} hours")
                    .ToList();
                    
                if (suspiciousDays.Count > 0)
                {
                    warnings.Add($"{suspiciousDays.Count} suspicious day(s)");
                    _logger.LogWarning($"Found {suspiciousDays.Count} days with >18 hours listening time");
                }
                
                // Check for overlapping imports
                var overlaps = await CheckForOverlappingImportsAsync(job.UserId, result.MinTimestamp, result.MaxTimestamp);
                if (overlaps.Count > 0)
                {
                    warnings.Add($"{overlaps.Count} overlap(s)");
                    _logger.LogWarning($"Found {overlaps.Count} overlapping imports");
                }
                
                result.Status = warnings.Count > 0 
                    ? $"Completed with warnings: {string.Join(" and ", warnings)}"
                    : "Completed";
                    
                _logger.LogInformation($"Processing completed. Imported: {result.ImportedItems}, Skipped: {result.SkippedItems}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error processing file: {ex.Message}");
                result.Status = $"Failed: {ex.Message}";
            }
            
            return result;
        }
        
        private bool IsValidTrack(SpotifyHistory item, out string trackId)
        {
            trackId = null;
            
            // Skip non-music content
            if (!string.IsNullOrEmpty(item.SpotifyEpisodeUri) || 
                !string.IsNullOrEmpty(item.AudiobookUri))
                return false;
                
            // Skip if no track URI
            if (string.IsNullOrEmpty(item.SpotifyTrackUri))
                return false;
                
            // Validate track duration (between 1ms and 1 hour)
            if (item.MsPlayed <= 0 || item.MsPlayed > 3600000)
                return false;
                
            // Extract track ID
            trackId = ExtractTrackIdFromUri(item.SpotifyTrackUri);
            return !string.IsNullOrEmpty(trackId);
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
        
        private async Task ProcessDocumentChunkAsync(List<Document> documents)
        {
            var tasks = documents.Select(doc => _container.UpsertItemAsync(doc, new PartitionKey(doc.userId)));
            await Task.WhenAll(tasks);
        }
        
        private async Task UpdateImportStatusAsync(int importHistoryId, string status)
        {
            using (var connection = new SqlConnection(_sqlConnectionString))
            {
                await connection.OpenAsync();
                var command = new SqlCommand(
                    "UPDATE ImportHistories SET Status = @status WHERE Id = @id",
                    connection);
                command.Parameters.AddWithValue("@id", importHistoryId);
                command.Parameters.AddWithValue("@status", status);
                
                await command.ExecuteNonQueryAsync();
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
        
        // Helper classes
        public class ProcessingResult
        {
            public int TotalItems { get; set; }
            public int ImportedItems { get; set; }
            public int SkippedItems { get; set; }
            public DateTime? MinTimestamp { get; set; }
            public DateTime? MaxTimestamp { get; set; }
            public string Status { get; set; }
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