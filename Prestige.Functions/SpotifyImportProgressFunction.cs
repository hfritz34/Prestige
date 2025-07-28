using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.Cosmos;
using System.Linq;

namespace Prestige.Functions
{
    public class SpotifyImportProgressFunction
    {
        private readonly ILogger _logger;
        private readonly Container _container;

        public SpotifyImportProgressFunction(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<SpotifyImportProgressFunction>();

            string connectionString = Environment.GetEnvironmentVariable("CosmosDBConnectionString");
            string databaseId = "MusicDB";
            string containerId = "RecentlyPlayed";

            var cosmosClient = new CosmosClient(connectionString);
            _container = cosmosClient.GetContainer(databaseId, containerId);
        }

        [Function("GetImportProgress")]
        public async Task<HttpResponseData> GetImportProgress(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "options", Route = "import-progress/{batchId}")] HttpRequestData req,
            string batchId)
        {
            // Handle CORS preflight requests
            if (req.Method.Equals("OPTIONS", StringComparison.OrdinalIgnoreCase))
            {
                var corsResponse = req.CreateResponse(HttpStatusCode.OK);
                corsResponse.Headers.Add("Access-Control-Allow-Origin", "*");
                corsResponse.Headers.Add("Access-Control-Allow-Methods", "GET, OPTIONS");
                corsResponse.Headers.Add("Access-Control-Allow-Headers", "Content-Type, Authorization");
                corsResponse.Headers.Add("Access-Control-Max-Age", "3600");
                return corsResponse;
            }

            _logger.LogInformation($"Getting import progress for batch: {batchId}");

            try
            {
                // Get count of documents for this batch
                var query = new QueryDefinition("SELECT VALUE COUNT(1) FROM c WHERE c.batchId = @batchId")
                    .WithParameter("@batchId", batchId);
                
                var iterator = _container.GetItemQueryIterator<int>(query);
                var processedCount = 0;
                
                while (iterator.HasMoreResults)
                {
                    var response = await iterator.ReadNextAsync();
                    processedCount = response.FirstOrDefault();
                    break;
                }

                // Get processing status for unprocessed documents
                var unprocessedQuery = new QueryDefinition(
                    "SELECT VALUE COUNT(1) FROM c WHERE c.batchId = @batchId AND (c.processed = false OR NOT IS_DEFINED(c.processed))")
                    .WithParameter("@batchId", batchId);
                
                var unprocessedIterator = _container.GetItemQueryIterator<int>(unprocessedQuery);
                var unprocessedCount = 0;
                
                while (unprocessedIterator.HasMoreResults)
                {
                    var response = await unprocessedIterator.ReadNextAsync();
                    unprocessedCount = response.FirstOrDefault();
                    break;
                }

                var result = new
                {
                    BatchId = batchId,
                    TotalDocuments = processedCount,
                    ProcessedDocuments = processedCount - unprocessedCount,
                    UnprocessedDocuments = unprocessedCount,
                    ProcessingPercentage = processedCount > 0 ? ((processedCount - unprocessedCount) * 100.0 / processedCount) : 0,
                    Status = unprocessedCount == 0 ? "Completed" : "Processing"
                };

                var httpResponse = req.CreateResponse(HttpStatusCode.OK);
                httpResponse.Headers.Add("Access-Control-Allow-Origin", "*");
                httpResponse.Headers.Add("Access-Control-Allow-Methods", "GET, OPTIONS");
                httpResponse.Headers.Add("Access-Control-Allow-Headers", "Content-Type");
                await httpResponse.WriteAsJsonAsync(result);
                return httpResponse;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting import progress: {ex.Message}");
                var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
                errorResponse.Headers.Add("Access-Control-Allow-Origin", "*");
                await errorResponse.WriteStringAsync($"Error retrieving import progress: {ex.Message}");
                return errorResponse;
            }
        }

        [Function("GetUserProcessingStatus")]
        public async Task<HttpResponseData> GetUserProcessingStatus(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "processing-status/{userId}")] HttpRequestData req,
            string userId)
        {
            _logger.LogInformation($"Getting processing status for user: {userId}");

            try
            {
                // Use query options to prevent timeout
                var queryOptions = new QueryRequestOptions
                {
                    MaxItemCount = -1,
                    MaxConcurrency = 1
                };

                // Get count of unprocessed documents for this user
                var query = new QueryDefinition(
                    "SELECT VALUE COUNT(1) FROM c WHERE c.userId = @userId AND (c.processed = false OR NOT IS_DEFINED(c.processed))")
                    .WithParameter("@userId", userId);
                
                var iterator = _container.GetItemQueryIterator<int>(query, requestOptions: queryOptions);
                var unprocessedCount = 0;
                
                var timeoutTask = Task.Delay(TimeSpan.FromSeconds(30));
                var queryTask = Task.Run(async () =>
                {
                    while (iterator.HasMoreResults)
                    {
                        var response = await iterator.ReadNextAsync();
                        unprocessedCount = response.FirstOrDefault();
                        break;
                    }
                });

                if (await Task.WhenAny(queryTask, timeoutTask) == timeoutTask)
                {
                    _logger.LogWarning($"Unprocessed count query timed out for user {userId}");
                    unprocessedCount = -1; // Indicate unknown count
                }
                else
                {
                    await queryTask;
                }

                // Get total count for user
                var totalQuery = new QueryDefinition("SELECT VALUE COUNT(1) FROM c WHERE c.userId = @userId")
                    .WithParameter("@userId", userId);
                
                var totalIterator = _container.GetItemQueryIterator<int>(totalQuery, requestOptions: queryOptions);
                var totalCount = 0;
                
                var totalTimeoutTask = Task.Delay(TimeSpan.FromSeconds(30));
                var totalQueryTask = Task.Run(async () =>
                {
                    while (totalIterator.HasMoreResults)
                    {
                        var response = await totalIterator.ReadNextAsync();
                        totalCount = response.FirstOrDefault();
                        break;
                    }
                });

                if (await Task.WhenAny(totalQueryTask, totalTimeoutTask) == totalTimeoutTask)
                {
                    _logger.LogWarning($"Total count query timed out for user {userId}");
                    totalCount = -1; // Indicate unknown count
                }
                else
                {
                    await totalQueryTask;
                }

                var result = new
                {
                    UserId = userId,
                    TotalTracks = totalCount >= 0 ? totalCount : 0,
                    ProcessedTracks = totalCount >= 0 && unprocessedCount >= 0 ? totalCount - unprocessedCount : 0,
                    UnprocessedTracks = unprocessedCount >= 0 ? unprocessedCount : 0,
                    ProcessingPercentage = totalCount > 0 && unprocessedCount >= 0 ? ((totalCount - unprocessedCount) * 100.0 / totalCount) : 0,
                    IsProcessing = unprocessedCount > 0,
                    QueryTimeout = totalCount < 0 || unprocessedCount < 0
                };

                var httpResponse = req.CreateResponse(HttpStatusCode.OK);
                httpResponse.Headers.Add("Access-Control-Allow-Origin", "*");
                httpResponse.Headers.Add("Access-Control-Allow-Methods", "GET, OPTIONS");
                httpResponse.Headers.Add("Access-Control-Allow-Headers", "Content-Type");
                await httpResponse.WriteAsJsonAsync(result);
                return httpResponse;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting user processing status: {ex.Message}");
                var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
                errorResponse.Headers.Add("Access-Control-Allow-Origin", "*");
                await errorResponse.WriteStringAsync($"Error retrieving processing status: {ex.Message}");
                return errorResponse;
            }
        }
    }
}