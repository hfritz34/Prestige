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

namespace Prestige.Functions
{
    public class SpotifyDataImportFunction
{
    private readonly ILogger _logger;
    private readonly Container _container;
    private readonly string _sqlConnectionString;

    public SpotifyDataImportFunction(ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<SpotifyDataImportFunction>();

        string connectionString = Environment.GetEnvironmentVariable("CosmosDBConnectionString");
        string databaseId = "MusicDB";
        string containerId = "RecentlyPlayed";

        var cosmosClient = new CosmosClient(connectionString);
        _container = cosmosClient.GetContainer(databaseId, containerId);
        
        _sqlConnectionString = Environment.GetEnvironmentVariable("SqlConnectionString");
    }

    [Function("SpotifyDataImportFunction")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestData req)
    {
        _logger.LogInformation("C# HTTP trigger function processed a request.");

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
            
            _logger.LogInformation($"Processing Spotify data import for user: {userId}");

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

            // Create a single batchId for this import session
            var batchId = Guid.NewGuid().ToString();
            var importStats = new ImportStatistics();

            // Read the multipart form data
            var reader = new MultipartReader(boundary, req.Body);
            MultipartSection section;
            var documentsToInsert = new List<Document>();
            var fileResults = new List<FileImportResult>();
            
            while ((section = await reader.ReadNextSectionAsync()) != null)
            {
                var hasContentDispositionHeader = ContentDispositionHeaderValue.TryParse(section.ContentDisposition, out var contentDisposition);
                if (!hasContentDispositionHeader)
                    continue;

                if (contentDisposition.DispositionType.Equals("form-data") && contentDisposition.FileName.HasValue)
                {
                    var fileName = contentDisposition.FileName.Value;
                    _logger.LogInformation($"Processing file: {fileName}");

                    using (var memoryStream = new MemoryStream())
                    {
                        await section.Body.CopyToAsync(memoryStream);
                        memoryStream.Position = 0;
                        
                        // Calculate file hash
                        var fileHash = await CalculateFileHashAsync(memoryStream);
                        _logger.LogInformation($"File {fileName} hash: {fileHash}");
                        
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
                        
                        // Create import history record
                        var importHistoryId = await CreateImportHistoryAsync(userId, fileHash, fileName, batchId);
                        
                        // Read the file content
                        memoryStream.Position = 0;
                        using (var readerStream = new StreamReader(memoryStream))
                        {
                            var data = await readerStream.ReadToEndAsync();
                            _logger.LogInformation($"File {fileName} content length: {data.Length} characters");
                            
                            var items = JsonConvert.DeserializeObject<List<SpotifyHistory>>(data);
                        
                            if (items == null || items.Count == 0)
                            {
                                _logger.LogWarning($"No items found in file: {fileName}. Deserialization returned null or empty list.");
                                await UpdateImportHistoryAsync(importHistoryId, 0, null, null, "Failed: No items found");
                                fileResults.Add(new FileImportResult 
                                { 
                                    FileName = fileName, 
                                    Status = "Failed - No items found",
                                    RecordCount = 0
                                });
                                continue;
                            }
                            
                            var fileDocuments = new List<Document>();
                            var fileStats = new FileImportStatistics { FileName = fileName };
                            DateTime? minTimestamp = null;
                            DateTime? maxTimestamp = null;
                            var dailyListeningTime = new Dictionary<string, int>(); // Track daily listening time in milliseconds
                            
                            importStats.TotalItems += items.Count;

                            foreach (var item in items)
                            {
                                // Skip if it's not a music track (episode or audiobook)
                                if (!string.IsNullOrEmpty(item.SpotifyEpisodeUri) || 
                                    !string.IsNullOrEmpty(item.AudiobookUri))
                                {
                                    importStats.SkippedItems++;
                                    _logger.LogInformation($"Skipped non-music content: {item.EpisodeName ?? item.AudiobookTitle}");
                                    continue;
                                }
                                
                                // Skip if no track URI
                                if (string.IsNullOrEmpty(item.SpotifyTrackUri))
                                {
                                    importStats.SkippedItems++;
                                    _logger.LogWarning($"Skipped item with no track URI");
                                    continue;
                                }
                                
                                // Validate track duration (between 1ms and 1 hour)
                                if (item.MsPlayed <= 0 || item.MsPlayed > 3600000) // 1 hour in milliseconds
                                {
                                    importStats.SkippedItems++;
                                    _logger.LogWarning($"Skipped track with invalid duration: {item.MsPlayed}ms");
                                    continue;
                                }
                                
                                // Extract track ID from Spotify URI (format: spotify:track:TRACK_ID)
                                var trackId = ExtractTrackIdFromUri(item.SpotifyTrackUri);
                                if (string.IsNullOrEmpty(trackId))
                                {
                                    importStats.SkippedItems++;
                                    _logger.LogWarning($"Failed to extract track ID from URI: {item.SpotifyTrackUri}");
                                    continue;
                                }
                            
                                // Create unique ID based on trackId, played_at timestamp, and batchId
                                var uniqueId = $"{trackId}_{item.Ts}_{batchId}";
                                
                                // Track timestamps and daily listening time
                                if (DateTime.TryParse(item.Ts, out var timestamp))
                                {
                                    if (minTimestamp == null || timestamp < minTimestamp)
                                        minTimestamp = timestamp;
                                    if (maxTimestamp == null || timestamp > maxTimestamp)
                                        maxTimestamp = timestamp;
                                        
                                    // Track daily listening time
                                    var dateKey = timestamp.Date.ToString("yyyy-MM-dd");
                                    if (!dailyListeningTime.ContainsKey(dateKey))
                                        dailyListeningTime[dateKey] = 0;
                                    dailyListeningTime[dateKey] += item.MsPlayed;
                                }
                                
                                var document = new Document
                                {
                                    id = uniqueId,
                                    batchId = batchId,
                                    userId = userId,
                                    trackId = trackId,
                                    duration_ms = item.MsPlayed,
                                    played_at = item.Ts  // ISO 8601 format timestamp
                                };

                                fileDocuments.Add(document);
                                documentsToInsert.Add(document);
                                fileStats.ImportedItems++;
                            }
                            
                            // Validate daily listening time (max 18 hours per day)
                            var suspiciousDays = new List<string>();
                            foreach (var day in dailyListeningTime)
                            {
                                var hoursListened = day.Value / (1000.0 * 60 * 60); // Convert ms to hours
                                if (hoursListened > 18) // More than 18 hours in a day is suspicious
                                {
                                    suspiciousDays.Add($"{day.Key}: {hoursListened:F1} hours");
                                    _logger.LogWarning($"Suspicious listening time on {day.Key}: {hoursListened:F1} hours");
                                }
                            }
                            
                            // Check for overlapping imports
                            var overlaps = await CheckForOverlappingImportsAsync(userId, minTimestamp, maxTimestamp);
                            if (overlaps.Count > 0 || suspiciousDays.Count > 0)
                            {
                                var warnings = new List<string>();
                                if (overlaps.Count > 0)
                                    warnings.Add($"{overlaps.Count} overlap(s)");
                                if (suspiciousDays.Count > 0)
                                    warnings.Add($"{suspiciousDays.Count} suspicious day(s)");
                                    
                                var warningMessage = string.Join(" and ", warnings);
                                _logger.LogWarning($"File {fileName} completed with warnings: {warningMessage}");
                                await UpdateImportHistoryAsync(importHistoryId, fileStats.ImportedItems, minTimestamp, maxTimestamp, $"Completed with warnings: {warningMessage}");
                                fileResults.Add(new FileImportResult 
                                { 
                                    FileName = fileName, 
                                    Status = $"Completed with {warningMessage}",
                                    RecordCount = fileStats.ImportedItems,
                                    Overlaps = overlaps,
                                    SuspiciousDays = suspiciousDays
                                });
                            }
                            else
                            {
                                // Update import history for this file
                                await UpdateImportHistoryAsync(importHistoryId, fileStats.ImportedItems, minTimestamp, maxTimestamp, "Completed");
                                fileResults.Add(new FileImportResult 
                                { 
                                    FileName = fileName, 
                                    Status = "Completed",
                                    RecordCount = fileStats.ImportedItems
                                });
                            }
                        }
                    }
                }
            }
            
            // Batch insert documents
            if (documentsToInsert.Count > 0)
            {
                _logger.LogInformation($"Inserting {documentsToInsert.Count} tracks for user {userId}");
                
                // Process in batches for better performance
                const int batchSize = 100;
                for (int i = 0; i < documentsToInsert.Count; i += batchSize)
                {
                    var batch = documentsToInsert.Skip(i).Take(batchSize);
                    var tasks = batch.Select(doc => _container.UpsertItemAsync(doc, new PartitionKey(doc.userId)));
                    await Task.WhenAll(tasks);
                    
                    importStats.ImportedItems += batch.Count();
                    _logger.LogInformation($"Progress: {importStats.ImportedItems}/{documentsToInsert.Count} tracks imported");
                }
            }

            var response = req.CreateResponse(HttpStatusCode.OK);
            var resultMessage = new
            {
                Summary = $"Import completed. Total items: {importStats.TotalItems}, Imported: {importStats.ImportedItems}, Skipped: {importStats.SkippedItems}",
                Files = fileResults
            };
            _logger.LogInformation($"Import completed: {JsonConvert.SerializeObject(resultMessage)}");
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
    
    private string ExtractTrackIdFromUri(string spotifyUri)
    {
        if (string.IsNullOrEmpty(spotifyUri))
            return null;
            
        // Format: spotify:track:TRACK_ID
        var parts = spotifyUri.Split(':');
        if (parts.Length == 3 && parts[0] == "spotify" && parts[1] == "track")
        {
            return parts[2];
        }
        
        return null;
    }
    
    private async Task<string> CalculateFileHashAsync(Stream stream)
    {
        using (var sha256 = SHA256.Create())
        {
            var hash = await sha256.ComputeHashAsync(stream);
            stream.Position = 0; // Reset stream position for reading
            return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
        }
    }
    
    private async Task<bool> IsFileDuplicateAsync(string userId, string fileHash)
    {
        using (var connection = new SqlConnection(_sqlConnectionString))
        {
            await connection.OpenAsync();
            var command = new SqlCommand(
                "SELECT COUNT(*) FROM ImportHistories WHERE UserId = @userId AND FileHash = @fileHash AND Status = 'Completed'",
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
                  AND Status = 'Completed'
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
    
    public class FileImportStatistics
    {
        public string FileName { get; set; }
        public int ImportedItems { get; set; }
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
