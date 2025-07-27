using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.Cosmos;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Prestige.Functions.Models;

namespace CosmosDBParser
{
    public class AutoProcessUnparsedTracks
    {
        private readonly ILogger _logger;

        public AutoProcessUnparsedTracks(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<AutoProcessUnparsedTracks>();
        }

        [Function("AutoProcessUnparsedTracks")]
        public async Task Run([TimerTrigger("0 */5 * * * *")] TimerInfo myTimer) // Every 5 minutes
        {
            _logger.LogInformation("AutoProcessUnparsedTracks timer trigger executed");

            try
            {
                // Connect to Cosmos DB
                string connectionString = Environment.GetEnvironmentVariable("CosmosDBConnectionString");
                var cosmosClient = new CosmosClient(connectionString);
                var container = cosmosClient.GetContainer("MusicDB", "RecentlyPlayed");

                // Query for unprocessed documents (batch of 100)
                var query = "SELECT * FROM c WHERE (c.processed = false OR NOT IS_DEFINED(c.processed)) ORDER BY c._ts ASC OFFSET 0 LIMIT 100";
                var queryDefinition = new QueryDefinition(query);
                
                using var feedIterator = container.GetItemQueryIterator<Document>(queryDefinition);
                var unprocessedDocs = new List<Document>();

                while (feedIterator.HasMoreResults)
                {
                    var response = await feedIterator.ReadNextAsync();
                    unprocessedDocs.AddRange(response);
                }

                if (unprocessedDocs.Count == 0)
                {
                    _logger.LogInformation("No unprocessed documents found");
                    return;
                }

                _logger.LogInformation($"Found {unprocessedDocs.Count} unprocessed documents. Processing...");

                // Process them using the same logic as CosmosDBParser
                await ProcessDocuments(unprocessedDocs, container);

                _logger.LogInformation($"Successfully processed {unprocessedDocs.Count} documents");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in AutoProcessUnparsedTracks: {ex.Message}");
            }
        }

        private async Task ProcessDocuments(List<Document> documents, Container container)
        {
            try
            {
                var client = await GetApiClientAsync();
                
                // Get unique track IDs for Spotify API call
                var uniqueTrackIds = documents.Select(d => d.trackId).Distinct().ToList();
                var trackIdsString = string.Join(",", uniqueTrackIds);
                
                _logger.LogInformation($"Requesting {uniqueTrackIds.Count} unique tracks from Spotify API");
                var trackMetadataResponse = await client.GetAsync($"spotify/tracks?ids={trackIdsString}");
                var responseContent = await trackMetadataResponse.Content.ReadAsStringAsync();
                
                if (!trackMetadataResponse.IsSuccessStatusCode)
                {
                    _logger.LogError($"Track metadata request failed: {trackMetadataResponse.StatusCode} - {responseContent}");
                    return;
                }

                // Group documents by user for batch processing
                var userGroups = documents.GroupBy(d => d.userId);
                
                foreach (var userGroup in userGroups)
                {
                    var userId = userGroup.Key;
                    var userTracks = userGroup.ToList();
                    
                    _logger.LogInformation($"Processing {userTracks.Count} tracks for user {userId}");
                    
                    // Send to API for processing
                    var requestBody = new
                    {
                        userId = userId,
                        tracks = userTracks.Select(track => new
                        {
                            trackId = track.trackId,
                            duration_ms = track.duration_ms,
                            played_at = track.played_at
                        }).ToList()
                    };
                    
                    var apiResponse = await client.PostAsJsonAsync("spotify/batch-process", requestBody);
                    var apiResponseContent = await apiResponse.Content.ReadAsStringAsync();
                    
                    if (apiResponse.IsSuccessStatusCode)
                    {
                        _logger.LogInformation($"Successfully sent {userTracks.Count} tracks to API for user {userId}");
                        
                        // Mark documents as processed in Cosmos DB
                        await MarkDocumentsAsProcessed(container, userTracks);
                    }
                    else
                    {
                        _logger.LogError($"API request failed for user {userId}: {apiResponse.StatusCode} - {apiResponseContent}");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error processing documents: {ex.Message}");
                throw;
            }
        }

        private async Task MarkDocumentsAsProcessed(Container container, List<Document> documents)
        {
            var tasks = documents.Select(async doc =>
            {
                try
                {
                    // Update the document to mark as processed
                    doc.processed = true;
                    doc.lastTriggered = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");
                    
                    await container.UpsertItemAsync(doc, new PartitionKey(doc.userId));
                    _logger.LogDebug($"Marked document {doc.id} as processed");
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error marking document {doc.id} as processed: {ex.Message}");
                }
            });

            await Task.WhenAll(tasks);
        }

        private async Task<HttpClient> GetApiClientAsync()
        {
            var client = new HttpClient();
            
            // Get API base URL from environment
            var apiBaseUrl = Environment.GetEnvironmentVariable("ApiBaseUrl");
            if (string.IsNullOrEmpty(apiBaseUrl))
            {
                throw new InvalidOperationException("ApiBaseUrl environment variable is not set");
            }
            
            client.BaseAddress = new Uri(apiBaseUrl);
            
            // Get M2M token for API authentication
            var auth0Domain = Environment.GetEnvironmentVariable("Auth0Domain");
            var clientId = Environment.GetEnvironmentVariable("FunctionM2MClientId");
            var clientSecret = Environment.GetEnvironmentVariable("FunctionM2MClientSecret");
            var audience = Environment.GetEnvironmentVariable("Auth0ApiAudience");

            if (string.IsNullOrEmpty(auth0Domain) || string.IsNullOrEmpty(clientId) || 
                string.IsNullOrEmpty(clientSecret) || string.IsNullOrEmpty(audience))
            {
                throw new InvalidOperationException("Auth0 configuration is incomplete");
            }

            var authClient = new HttpClient();
            var tokenRequest = new
            {
                client_id = clientId,
                client_secret = clientSecret,
                audience = audience,
                grant_type = "client_credentials"
            };

            var tokenResponse = await authClient.PostAsJsonAsync($"https://{auth0Domain}/oauth/token", tokenRequest);
            var tokenContent = await tokenResponse.Content.ReadAsStringAsync();
            
            if (!tokenResponse.IsSuccessStatusCode)
            {
                throw new InvalidOperationException($"Failed to get Auth0 token: {tokenResponse.StatusCode} - {tokenContent}");
            }

            var tokenData = System.Text.Json.JsonSerializer.Deserialize<JsonElement>(tokenContent);
            var accessToken = tokenData.GetProperty("access_token").GetString();

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            return client;
        }
    }

    // Note: Document class is defined in CosmosDBParser.cs
}