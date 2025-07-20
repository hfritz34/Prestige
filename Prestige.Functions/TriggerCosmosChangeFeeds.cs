using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.Cosmos;
using System.Net;

namespace CosmosDBParser
{
    public class TriggerCosmosChangeFeeds
    {
        private readonly ILogger _logger;

        public TriggerCosmosChangeFeeds(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<TriggerCosmosChangeFeeds>();
        }

        [Function("TriggerCosmosChangeFeeds")]
        public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req)
        {
            _logger.LogInformation("TriggerCosmosChangeFeeds HTTP trigger function processed a request");
            
            try
            {
                // Parse query parameters
                var query = System.Web.HttpUtility.ParseQueryString(req.Url.Query);
                var userId = query["userId"];
                var batchSize = int.TryParse(query["batchSize"], out var bs) ? bs : 100;
                
                _logger.LogInformation($"Triggering change feeds - UserId: {userId}, BatchSize: {batchSize}");

                // Get Cosmos DB client
                var cosmosConnectionString = Environment.GetEnvironmentVariable("CosmosDBConnectionString");
                if (string.IsNullOrEmpty(cosmosConnectionString))
                {
                    throw new Exception("CosmosDBConnectionString setting not found");
                }

                var cosmosClient = new CosmosClient(cosmosConnectionString);
                var database = cosmosClient.GetDatabase("MusicDB");
                var container = database.GetContainer("RecentlyPlayed");

                // Build query to get unprocessed documents
                var sqlQuery = "SELECT * FROM c WHERE (c.processed = false OR NOT IS_DEFINED(c.processed))";
                var queryParams = new List<(string, object)>();
                
                if (!string.IsNullOrEmpty(userId))
                {
                    sqlQuery += " AND c.userId = @userId";
                    queryParams.Add(("@userId", userId));
                }
                
                _logger.LogInformation($"Executing query: {sqlQuery}");

                // Create query definition
                var queryDefinition = new QueryDefinition(sqlQuery);
                foreach (var (paramName, paramValue) in queryParams)
                {
                    queryDefinition.WithParameter(paramName, paramValue);
                }

                // Execute query and trigger change feeds by updating documents
                var iterator = container.GetItemQueryIterator<Document>(queryDefinition);
                var totalProcessed = 0;
                var totalTriggered = 0;

                while (iterator.HasMoreResults)
                {
                    var response = await iterator.ReadNextAsync();
                    var documents = response.ToList();
                    
                    if (documents.Count == 0)
                        break;

                    _logger.LogInformation($"Triggering change feeds for batch of {documents.Count} documents");
                    
                    // Update each document to trigger the change feed
                    var tasks = documents.Take(batchSize).Select(async doc =>
                    {
                        try
                        {
                            // Add a timestamp to trigger the change feed
                            doc.lastTriggered = DateTime.UtcNow.ToString("O");
                            await container.ReplaceItemAsync(doc, doc.id, new PartitionKey(doc.userId));
                            return true;
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, $"Failed to trigger document {doc.id}");
                            return false;
                        }
                    });
                    
                    var results = await Task.WhenAll(tasks);
                    var triggered = results.Count(r => r);
                    
                    totalProcessed += documents.Count;
                    totalTriggered += triggered;
                    
                    _logger.LogInformation($"Batch completed: {triggered} triggered. Total so far: {totalTriggered} triggered out of {totalProcessed}");
                    
                    // Small delay between batches
                    await Task.Delay(1000);
                    
                    // Limit to batch size per call to avoid overwhelming the system
                    if (totalTriggered >= batchSize)
                        break;
                }

                var responseMessage = $"Change feed triggering completed. Total processed: {totalProcessed}, Total triggered: {totalTriggered}";
                _logger.LogInformation(responseMessage);

                var httpResponse = req.CreateResponse(HttpStatusCode.OK);
                await httpResponse.WriteStringAsync(responseMessage);
                return httpResponse;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "TriggerCosmosChangeFeeds execution failed");
                var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
                await errorResponse.WriteStringAsync($"Error: {ex.Message}");
                return errorResponse;
            }
        }
    }
}