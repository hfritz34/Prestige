using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.Cosmos;
using System.Net;
using Prestige.Functions.Models;
using System.Collections.Concurrent;

namespace CosmosDBParser
{
    public class ManualCosmosDBProcessor
    {
        private readonly ILogger _logger;

        // Static token cache to persist across function executions
        private static readonly ConcurrentDictionary<string, CachedToken> _tokenCache = new();
        
        // Token cache entry
        private class CachedToken
        {
            public string AccessToken { get; set; } = string.Empty;
            public DateTime ExpiresAt { get; set; }
            public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
        }

        public ManualCosmosDBProcessor(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<ManualCosmosDBProcessor>();
        }

        [Function("ManualCosmosDBProcessor")]
        public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req)
        {
            _logger.LogInformation("ManualCosmosDBProcessor HTTP trigger function processed a request");
            
            try
            {
                // Parse query parameters
                var query = System.Web.HttpUtility.ParseQueryString(req.Url.Query);
                var userId = query["userId"];
                var batchSize = int.TryParse(query["batchSize"], out var bs) ? bs : 100;
                var skipProcessed = bool.TryParse(query["skipProcessed"], out var sp) ? sp : true;
                
                _logger.LogInformation($"Processing documents - UserId: {userId}, BatchSize: {batchSize}, SkipProcessed: {skipProcessed}");

                // Get Cosmos DB client
                var cosmosConnectionString = Environment.GetEnvironmentVariable("CosmosDBConnectionString");
                if (string.IsNullOrEmpty(cosmosConnectionString))
                {
                    throw new Exception("CosmosDBConnectionString setting not found");
                }

                var cosmosClient = new CosmosClient(cosmosConnectionString);
                var database = cosmosClient.GetDatabase("MusicDB");
                var container = database.GetContainer("RecentlyPlayed");

                // Build query
                var sqlQuery = "SELECT * FROM c";
                var queryParams = new List<(string, object)>();
                
                if (!string.IsNullOrEmpty(userId))
                {
                    sqlQuery += " WHERE c.userId = @userId";
                    queryParams.Add(("@userId", userId));
                }
                
                if (skipProcessed)
                {
                    sqlQuery += string.IsNullOrEmpty(userId) ? " WHERE" : " AND";
                    sqlQuery += " (c.processed = false OR NOT IS_DEFINED(c.processed))";
                }
                
                _logger.LogInformation($"Executing query: {sqlQuery}");

                // Create query definition
                var queryDefinition = new QueryDefinition(sqlQuery);
                foreach (var (paramName, paramValue) in queryParams)
                {
                    queryDefinition.WithParameter(paramName, paramValue);
                }

                // Execute query and process in batches
                var iterator = container.GetItemQueryIterator<Document>(queryDefinition);
                var totalProcessed = 0;
                var totalSuccessful = 0;
                var totalFailed = 0;

                while (iterator.HasMoreResults)
                {
                    var response = await iterator.ReadNextAsync();
                    var documents = response.ToList();
                    
                    if (documents.Count == 0)
                        break;

                    _logger.LogInformation($"Processing batch of {documents.Count} documents");
                    
                    // Process this batch using the same logic as CosmosDBParser
                    var (successful, failed) = await ProcessDocumentBatch(documents);
                    
                    totalProcessed += documents.Count;
                    totalSuccessful += successful;
                    totalFailed += failed;
                    
                    // Mark documents as processed if they were successful
                    if (successful > 0)
                    {
                        await MarkDocumentsAsProcessed(container, documents.Take(successful));
                    }
                    
                    _logger.LogInformation($"Batch completed: {successful} successful, {failed} failed. Total so far: {totalSuccessful} successful, {totalFailed} failed");
                    
                    // Small delay between batches
                    await Task.Delay(500);
                }

                var responseMessage = $"Manual processing completed. Total: {totalProcessed}, Successful: {totalSuccessful}, Failed: {totalFailed}";
                _logger.LogInformation(responseMessage);

                var httpResponse = req.CreateResponse(HttpStatusCode.OK);
                await httpResponse.WriteStringAsync(responseMessage);
                return httpResponse;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ManualCosmosDBProcessor execution failed");
                var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
                await errorResponse.WriteStringAsync($"Error: {ex.Message}");
                return errorResponse;
            }
        }

        private async Task<(int successful, int failed)> ProcessDocumentBatch(List<Document> documents)
        {
            try
            {
                var client = await GetApiClientAsync();
                
                _logger.LogInformation($"Processing {documents.Count} documents directly - will handle missing tracks gracefully");

                // Group documents by user for batch processing
                var userGroups = documents.GroupBy(d => d.userId);
                var totalSuccessful = 0;
                var totalFailed = 0;
                
                foreach (var userGroup in userGroups)
                {
                    var userId = userGroup.Key;
                    var userTracks = userGroup.ToList();
                    
                    // Process tracks in smaller batches
                    const int batchSize = 25; // Smaller batches for manual processing
                    var batches = userTracks
                        .Select((track, index) => new { track, index })
                        .GroupBy(x => x.index / batchSize)
                        .Select(g => g.Select(x => x.track).ToList());
                    
                    foreach (var batch in batches)
                    {
                        var batchTasks = batch.Select(async doc =>
                        {
                            try
                            {
                                // First, try to post the track - if it fails with "Track not found", 
                                // we need to create the track first via Spotify API
                                var userTrackResponse = await client.PostAsJsonAsync($"prestige/{doc.userId}/tracks", new
                                {
                                    TrackId = doc.trackId,
                                    TotalTime = doc.duration_ms / 1000
                                });

                                if (userTrackResponse.IsSuccessStatusCode)
                                {
                                    return true;
                                }

                                var errorContent = await userTrackResponse.Content.ReadAsStringAsync();
                                
                                // If track not found, try to create it via Spotify API first
                                if (userTrackResponse.StatusCode == System.Net.HttpStatusCode.NotFound && 
                                    errorContent.Contains("Track not found"))
                                {
                                    _logger.LogInformation($"Track {doc.trackId} not found, attempting to create via Spotify API");
                                    
                                    // Try to create the track via Spotify API
                                    var spotifyResponse = await client.GetAsync($"spotify/track/{doc.trackId}");
                                    if (spotifyResponse.IsSuccessStatusCode)
                                    {
                                        _logger.LogInformation($"Successfully created track {doc.trackId} via Spotify API");
                                        
                                        // Now retry the user track posting
                                        var retryResponse = await client.PostAsJsonAsync($"prestige/{doc.userId}/tracks", new
                                        {
                                            TrackId = doc.trackId,
                                            TotalTime = doc.duration_ms / 1000
                                        });
                                        
                                        if (retryResponse.IsSuccessStatusCode)
                                        {
                                            return true;
                                        }
                                        else
                                        {
                                            var retryError = await retryResponse.Content.ReadAsStringAsync();
                                            _logger.LogError($"Failed posting track {doc.trackId} after Spotify creation: {retryResponse.StatusCode} - {retryError}");
                                            return false;
                                        }
                                    }
                                    else
                                    {
                                        var spotifyError = await spotifyResponse.Content.ReadAsStringAsync();
                                        _logger.LogError($"Failed to create track {doc.trackId} via Spotify API: {spotifyResponse.StatusCode} - {spotifyError}");
                                        return false;
                                    }
                                }
                                else
                                {
                                    _logger.LogError($"Failed posting track {doc.trackId}: {userTrackResponse.StatusCode} - {errorContent}");
                                    return false;
                                }
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, $"Error processing track {doc.trackId} for user {doc.userId}");
                                return false;
                            }
                        });
                        
                        var results = await Task.WhenAll(batchTasks);
                        totalSuccessful += results.Count(r => r);
                        totalFailed += results.Count(r => !r);
                        
                        // Small delay between batches
                        await Task.Delay(100);
                    }
                }
                
                return (totalSuccessful, totalFailed);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing document batch");
                return (0, documents.Count);
            }
        }

        private async Task MarkDocumentsAsProcessed(Container container, IEnumerable<Document> documents)
        {
            try
            {
                var tasks = documents.Select(async doc =>
                {
                    try
                    {
                        // Update the document to mark it as processed
                        doc.processed = true;
                        await container.ReplaceItemAsync(doc, doc.id, new PartitionKey(doc.userId));
                        return true;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, $"Failed to mark document {doc.id} as processed");
                        return false;
                    }
                });
                
                await Task.WhenAll(tasks);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking documents as processed");
            }
        }

        private async Task<HttpClient> GetApiClientAsync()
        {
            // Create cache key based on environment settings
            var m2mClientId = Environment.GetEnvironmentVariable("FunctionM2MClientId") ?? throw new Exception("FunctionM2MClientId setting not found");
            var apiAudience = Environment.GetEnvironmentVariable("Auth0ApiAudience") ?? throw new Exception("Auth0ApiAudience setting not found");
            var cacheKey = $"{m2mClientId}_{apiAudience}";
            
            // Check if we have a valid cached token
            if (_tokenCache.TryGetValue(cacheKey, out var cachedToken) && !cachedToken.IsExpired)
            {
                _logger.LogInformation("Using cached Auth0 M2M token");
                var cachedClient = new HttpClient();
                cachedClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", cachedToken.AccessToken);
                cachedClient.BaseAddress = new Uri(Environment.GetEnvironmentVariable("ApiBaseUrl") ?? throw new Exception("ApiBaseUrl setting not found"));
                return cachedClient;
            }

            _logger.LogInformation("Requesting new Auth0 M2M token for Prestige API");
            var auth0Client = new HttpClient();
            
            var auth0Domain = Environment.GetEnvironmentVariable("Auth0Domain");
            if (!string.IsNullOrEmpty(auth0Domain) && !auth0Domain.EndsWith("/"))
            {
                auth0Domain += "/";
            }
            
            var m2mClientSecret = Environment.GetEnvironmentVariable("FunctionM2MClientSecret") ?? throw new Exception("FunctionM2MClientSecret setting not found");

            var request = new HttpRequestMessage(HttpMethod.Post, $"https://{auth0Domain}oauth/token");
            request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                {"grant_type", "client_credentials"},
                {"client_id", m2mClientId},
                {"client_secret", m2mClientSecret},
                {"audience", apiAudience}
            });

            var tokenResponse = await auth0Client.SendAsync(request);
            var responseContent = await tokenResponse.Content.ReadAsStringAsync();
            
            if (!tokenResponse.IsSuccessStatusCode)
            {
                _logger.LogError($"Auth0 M2M token error: {responseContent}");
                tokenResponse.EnsureSuccessStatusCode();
            }
            
            var tokenObject = await tokenResponse.Content.ReadFromJsonAsync<Dictionary<string, object>>();
            if (tokenObject == null || !tokenObject.TryGetValue("access_token", out object? accessTokenValue))
            {
                throw new Exception("Failed to retrieve access token from Auth0 M2M response.");
            }
            
            var accessToken = accessTokenValue.ToString() ?? throw new Exception("Access token is null");
            
            // Parse expires_in from response (typically 86400 seconds = 24 hours)
            var expiresIn = 3600; // Default to 1 hour if not specified
            if (tokenObject.TryGetValue("expires_in", out object? expiresValue) && int.TryParse(expiresValue.ToString(), out var parsedExpires))
            {
                expiresIn = parsedExpires;
            }
            
            // Cache the token with 5 minute buffer before actual expiry
            var expiresAt = DateTime.UtcNow.AddSeconds(expiresIn - 300);
            _tokenCache.AddOrUpdate(cacheKey, new CachedToken 
            { 
                AccessToken = accessToken, 
                ExpiresAt = expiresAt 
            }, (key, old) => new CachedToken 
            { 
                AccessToken = accessToken, 
                ExpiresAt = expiresAt 
            });
            
            _logger.LogInformation($"Successfully obtained and cached M2M access token. Expires at: {expiresAt:yyyy-MM-dd HH:mm:ss} UTC");

            var apiClient = new HttpClient();
            apiClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            apiClient.BaseAddress = new Uri(Environment.GetEnvironmentVariable("ApiBaseUrl") ?? throw new Exception("ApiBaseUrl setting not found"));

            return apiClient;
        }
    }

}