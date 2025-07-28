using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;

namespace Prestige.Functions.Helpers
{
    public class CosmosBulkImporter
    {
        private readonly Container _container;
        private readonly ILogger _logger;
        private readonly CosmosClient _cosmosClient;

        public CosmosBulkImporter(string connectionString, string databaseId, string containerId, ILogger logger)
        {
            _logger = logger;
            
            // Configure Cosmos client for bulk operations
            var cosmosClientOptions = new CosmosClientOptions
            {
                ConnectionMode = ConnectionMode.Direct,
                AllowBulkExecution = true,
                MaxRetryAttemptsOnRateLimitedRequests = 10,
                MaxRetryWaitTimeOnRateLimitedRequests = TimeSpan.FromSeconds(60),
                RequestTimeout = TimeSpan.FromSeconds(60),
                // Increase throughput for better performance
                MaxRequestsPerTcpConnection = 8,
                MaxTcpConnectionsPerEndpoint = 16
            };
            
            _cosmosClient = new CosmosClient(connectionString, cosmosClientOptions);
            _container = _cosmosClient.GetContainer(databaseId, containerId);
        }

        public async Task<BulkImportResult> BulkImportAsync<T>(List<T> items, Func<T, string> getPartitionKey)
        {
            var result = new BulkImportResult();
            const int maxConcurrency = 50; // Process up to 50 items concurrently
            
            var semaphore = new System.Threading.SemaphoreSlim(maxConcurrency);
            var tasks = new List<Task<ItemResponse<T>>>();
            
            foreach (var item in items)
            {
                await semaphore.WaitAsync();
                
                var task = Task.Run(async () =>
                {
                    try
                    {
                        var partitionKey = getPartitionKey(item);
                        var response = await _container.UpsertItemAsync(item, new PartitionKey(partitionKey));
                        semaphore.Release();
                        return response;
                    }
                    catch (Exception)
                    {
                        semaphore.Release();
                        throw;
                    }
                });
                
                tasks.Add(task);
            }
            
            // Wait for all tasks to complete
            var responses = await Task.WhenAll(tasks.Select(HandleTaskAsync));
            
            // Count successes and failures
            result.SuccessCount = responses.Count(r => r.success);
            result.FailureCount = responses.Count(r => !r.success);
            result.TotalRequestCharge = responses.Where(r => r.requestCharge.HasValue).Sum(r => r.requestCharge.Value);
            
            _logger.LogInformation($"Bulk import completed: {result.SuccessCount} succeeded, {result.FailureCount} failed, {result.TotalRequestCharge:F2} RUs consumed");
            
            return result;
        }
        
        private async Task<(bool success, double? requestCharge)> HandleTaskAsync<T>(Task<ItemResponse<T>> task)
        {
            try
            {
                var response = await task;
                return (true, response.RequestCharge);
            }
            catch (CosmosException ex)
            {
                _logger.LogError($"Cosmos error: {ex.StatusCode} - {ex.Message}");
                return (false, ex.RequestCharge);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Unexpected error: {ex.Message}");
                return (false, null);
            }
        }
        
        public void Dispose()
        {
            _cosmosClient?.Dispose();
        }
    }
    
    public class BulkImportResult
    {
        public int SuccessCount { get; set; }
        public int FailureCount { get; set; }
        public double TotalRequestCharge { get; set; }
    }
}