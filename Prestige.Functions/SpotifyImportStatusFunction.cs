using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace Prestige.Functions
{
    public class SpotifyImportStatusFunction
    {
        private readonly ILogger _logger;
        private readonly string _sqlConnectionString;

        public SpotifyImportStatusFunction(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<SpotifyImportStatusFunction>();
            _sqlConnectionString = Environment.GetEnvironmentVariable("SqlConnectionString");
        }

        [Function("GetImportStatus")]
        public async Task<HttpResponseData> GetImportStatus(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "import-status/{batchId}")] HttpRequestData req,
            string batchId)
        {
            _logger.LogInformation($"Getting import status for batch: {batchId}");

            try
            {
                var imports = await GetImportsByBatchIdAsync(batchId);
                
                if (imports.Count == 0)
                {
                    var notFoundResponse = req.CreateResponse(HttpStatusCode.NotFound);
                    notFoundResponse.Headers.Add("Access-Control-Allow-Origin", "*");
                    await notFoundResponse.WriteStringAsync($"No imports found for batch ID: {batchId}");
                    return notFoundResponse;
                }

                var response = req.CreateResponse(HttpStatusCode.OK);
                response.Headers.Add("Access-Control-Allow-Origin", "*");
                response.Headers.Add("Access-Control-Allow-Methods", "GET, POST, OPTIONS");
                response.Headers.Add("Access-Control-Allow-Headers", "Content-Type");
                await response.WriteAsJsonAsync(new
                {
                    BatchId = batchId,
                    Files = imports,
                    Summary = GenerateSummary(imports)
                });
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting import status: {ex.Message}");
                var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
                errorResponse.Headers.Add("Access-Control-Allow-Origin", "*");
                await errorResponse.WriteStringAsync($"Error retrieving import status: {ex.Message}");
                return errorResponse;
            }
        }

        [Function("GetUserImportHistory")]
        public async Task<HttpResponseData> GetUserImportHistory(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "import-history/{userId}")] HttpRequestData req,
            string userId)
        {
            _logger.LogInformation($"Getting import history for user: {userId}");

            try
            {
                var limit = 50; // Default limit
                var limitStr = req.Query["limit"];
                if (!string.IsNullOrEmpty(limitStr) && int.TryParse(limitStr, out var parsedLimit))
                {
                    limit = Math.Min(parsedLimit, 100); // Cap at 100
                }

                var imports = await GetImportsByUserIdAsync(userId, limit);

                var response = req.CreateResponse(HttpStatusCode.OK);
                response.Headers.Add("Access-Control-Allow-Origin", "*");
                response.Headers.Add("Access-Control-Allow-Methods", "GET, POST, OPTIONS");
                response.Headers.Add("Access-Control-Allow-Headers", "Content-Type");
                await response.WriteAsJsonAsync(new
                {
                    UserId = userId,
                    Imports = imports,
                    Count = imports.Count
                });
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting user import history: {ex.Message}");
                var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
                errorResponse.Headers.Add("Access-Control-Allow-Origin", "*");
                await errorResponse.WriteStringAsync($"Error retrieving import history: {ex.Message}");
                return errorResponse;
            }
        }

        private async Task<List<ImportStatus>> GetImportsByBatchIdAsync(string batchId)
        {
            var imports = new List<ImportStatus>();
            
            using (var connection = new SqlConnection(_sqlConnectionString))
            {
                await connection.OpenAsync();
                var command = new SqlCommand(
                    @"SELECT Id, UserId, FileName, ImportDate, Status, RecordCount, MinTimestamp, MaxTimestamp
                      FROM ImportHistories
                      WHERE BatchId = @batchId
                      ORDER BY ImportDate DESC",
                    connection);
                command.Parameters.AddWithValue("@batchId", batchId);
                
                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        imports.Add(new ImportStatus
                        {
                            Id = reader.GetInt32(0),
                            UserId = reader.GetString(1),
                            FileName = reader.GetString(2),
                            ImportDate = reader.GetDateTime(3),
                            Status = reader.GetString(4),
                            RecordCount = reader.GetInt32(5),
                            MinTimestamp = reader.IsDBNull(6) ? null : reader.GetDateTime(6),
                            MaxTimestamp = reader.IsDBNull(7) ? null : reader.GetDateTime(7)
                        });
                    }
                }
            }
            
            return imports;
        }

        private async Task<List<ImportStatus>> GetImportsByUserIdAsync(string userId, int limit)
        {
            var imports = new List<ImportStatus>();
            
            using (var connection = new SqlConnection(_sqlConnectionString))
            {
                await connection.OpenAsync();
                var command = new SqlCommand(
                    @"SELECT TOP (@limit) Id, BatchId, FileName, ImportDate, Status, RecordCount, MinTimestamp, MaxTimestamp
                      FROM ImportHistories
                      WHERE UserId = @userId
                      ORDER BY ImportDate DESC",
                    connection);
                command.Parameters.AddWithValue("@userId", userId);
                command.Parameters.AddWithValue("@limit", limit);
                
                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        imports.Add(new ImportStatus
                        {
                            Id = reader.GetInt32(0),
                            BatchId = reader.GetString(1),
                            FileName = reader.GetString(2),
                            ImportDate = reader.GetDateTime(3),
                            Status = reader.GetString(4),
                            RecordCount = reader.GetInt32(5),
                            MinTimestamp = reader.IsDBNull(6) ? null : reader.GetDateTime(6),
                            MaxTimestamp = reader.IsDBNull(7) ? null : reader.GetDateTime(7)
                        });
                    }
                }
            }
            
            return imports;
        }

        private ImportSummary GenerateSummary(List<ImportStatus> imports)
        {
            return new ImportSummary
            {
                TotalFiles = imports.Count,
                CompletedFiles = imports.Count(i => i.Status.StartsWith("Completed")),
                FailedFiles = imports.Count(i => i.Status.StartsWith("Failed")),
                ProcessingFiles = imports.Count(i => i.Status == "Processing"),
                QueuedFiles = imports.Count(i => i.Status == "Queued"),
                TotalRecords = imports.Sum(i => i.RecordCount),
                AllCompleted = imports.All(i => i.Status.StartsWith("Completed") || i.Status.StartsWith("Failed"))
            };
        }

        // Helper classes
        public class ImportStatus
        {
            public int Id { get; set; }
            public string UserId { get; set; }
            public string BatchId { get; set; }
            public string FileName { get; set; }
            public DateTime ImportDate { get; set; }
            public string Status { get; set; }
            public int RecordCount { get; set; }
            public DateTime? MinTimestamp { get; set; }
            public DateTime? MaxTimestamp { get; set; }
        }

        public class ImportSummary
        {
            public int TotalFiles { get; set; }
            public int CompletedFiles { get; set; }
            public int FailedFiles { get; set; }
            public int ProcessingFiles { get; set; }
            public int QueuedFiles { get; set; }
            public int TotalRecords { get; set; }
            public bool AllCompleted { get; set; }
        }
    }
}