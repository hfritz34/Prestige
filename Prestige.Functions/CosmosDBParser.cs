using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Prestige.Functions.Models;
using System.Collections.Concurrent;

namespace CosmosDBParser
{
   public class CosmosDBParser
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

       public CosmosDBParser(ILoggerFactory loggerFactory)
       {
           _logger = loggerFactory.CreateLogger<CosmosDBParser>();
       }

       [Function("CosmosDBParser")]
       public async Task Run([CosmosDBTrigger(
           databaseName: "MusicDB",
           containerName: "RecentlyPlayed",
           Connection = "CosmosDBConnectionString", 
           LeaseContainerName = "leases",
           CreateLeaseContainerIfNotExists =  true )] IReadOnlyList<Document> data)
       {
           _logger.LogInformation($"CosmosDBParser triggered with {data.Count} documents at {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
           
           try 
           {
               var client = await GetApiClientAsync();
               
               _logger.LogInformation($"Processing {data.Count} documents in batches");
               
               // Get unique track IDs for Spotify API call
               var uniqueTrackIds = data.Select(d => d.trackId).Distinct().ToList();
               var trackIdsString = string.Join(",", uniqueTrackIds);
               
               _logger.LogInformation($"Requesting {uniqueTrackIds.Count} unique tracks from Spotify API");
               var trackMetadataResponse = await client.GetAsync($"spotify/tracks?ids={trackIdsString}");
               var responseContent = await trackMetadataResponse.Content.ReadAsStringAsync();
               
               if (!trackMetadataResponse.IsSuccessStatusCode)
               {
                   if (trackMetadataResponse.StatusCode == System.Net.HttpStatusCode.NotFound)
                   {
                       _logger.LogWarning($"Some tracks not found on Spotify (404): {responseContent}");
                       // Continue processing - some tracks may be deleted/unavailable
                   }
                   else
                   {
                       _logger.LogError($"Track metadata request failed: {trackMetadataResponse.StatusCode} - {responseContent}");
                       trackMetadataResponse.EnsureSuccessStatusCode();
                   }
               }

               // Group documents by user for batch processing
               var userGroups = data.GroupBy(d => d.userId);
               
               foreach (var userGroup in userGroups)
               {
                   var userId = userGroup.Key;
                   var userTracks = userGroup.ToList();
                   
                   _logger.LogInformation($"Processing {userTracks.Count} tracks for user {userId} in batches");
                   
                   // Process tracks in smaller batches to avoid overwhelming the API
                   const int batchSize = 50;
                   var batches = userTracks
                       .Select((track, index) => new { track, index })
                       .GroupBy(x => x.index / batchSize)
                       .Select(g => g.Select(x => x.track).ToList());
                   
                   var batchNumber = 1;
                   foreach (var batch in batches)
                   {
                       _logger.LogInformation($"Processing batch {batchNumber} of {batch.Count} tracks for user {userId}");
                       
                       // Create concurrent requests for this batch
                       var batchTasks = batch.Select(async doc =>
                       {
                           try
                           {
                               var userTrackResponse = await client.PostAsJsonAsync($"prestige/{doc.userId}/tracks", new
                               {
                                   TrackId = doc.trackId,
                                   TotalTime = doc.duration_ms / 1000
                               });

                               if (!userTrackResponse.IsSuccessStatusCode)
                               {
                                   var errorContent = await userTrackResponse.Content.ReadAsStringAsync();
                                   if (userTrackResponse.StatusCode == System.Net.HttpStatusCode.NotFound)
                                   {
                                       _logger.LogWarning($"Track {doc.trackId} not found (404) - skipping: {errorContent}");
                                   }
                                   else if (userTrackResponse.StatusCode == System.Net.HttpStatusCode.BadRequest)
                                   {
                                       _logger.LogWarning($"Invalid track data for {doc.trackId} (400) - skipping: {errorContent}");
                                   }
                                   else if (userTrackResponse.StatusCode == System.Net.HttpStatusCode.InternalServerError)
                                   {
                                       _logger.LogError($"API Internal Server Error for track {doc.trackId} user {doc.userId}: {errorContent}");
                                       // Check if it's a database-related error
                                       if (errorContent.Contains("LastUpdatedAt") || errorContent.Contains("DateTime"))
                                       {
                                           _logger.LogError($"Possible LastUpdatedAt migration issue detected for user {doc.userId}");
                                       }
                                   }
                                   else
                                   {
                                       _logger.LogError($"Failed posting track {doc.trackId}: {userTrackResponse.StatusCode} - {errorContent}");
                                   }
                                   return false;
                               }
                               
                               _logger.LogDebug($"Successfully processed track {doc.trackId} for user {doc.userId}");
                               return true;
                           }
                           catch (HttpRequestException httpEx)
                           {
                               _logger.LogError(httpEx, $"HTTP request failed for track {doc.trackId} user {doc.userId}: {httpEx.Message}");
                               return false;
                           }
                           catch (TaskCanceledException tcEx)
                           {
                               _logger.LogError(tcEx, $"Request timeout for track {doc.trackId} user {doc.userId}");
                               return false;
                           }
                           catch (Exception ex)
                           {
                               _logger.LogError(ex, $"Unexpected error processing track {doc.trackId} for user {doc.userId}: {ex.Message}");
                               return false;
                           }
                       });
                       
                       // Wait for all requests in this batch to complete
                       var results = await Task.WhenAll(batchTasks);
                       var successCount = results.Count(r => r);
                       var failureCount = results.Count(r => !r);
                       
                       _logger.LogInformation($"Batch {batchNumber} for user {userId} completed: {successCount} successful, {failureCount} failed");
                   
                   // If we have a high failure rate, log a warning
                   if (failureCount > 0 && (double)failureCount / results.Length > 0.5)
                   {
                       _logger.LogWarning($"High failure rate ({failureCount}/{results.Length}) for user {userId} in batch {batchNumber}. Check API health and database state.");
                   }
                       batchNumber++;
                       
                       // Small delay between batches to avoid overwhelming the API
                       if (batchNumber <= batches.Count())
                       {
                           await Task.Delay(100);
                       }
                   }
               }
           }
           catch (Exception ex)
           {
               _logger.LogError(ex, "CosmosDBParser execution failed");
               throw;
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
           
           // Ensure domain has trailing slash for consistency
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

           try 
           {
               var tokenResponse = await auth0Client.SendAsync(request);
               var responseContent = await tokenResponse.Content.ReadAsStringAsync();
               _logger.LogInformation($"Auth0 M2M token response: {tokenResponse.StatusCode}");
               
               if (!tokenResponse.IsSuccessStatusCode)
               {
                   _logger.LogError($"Auth0 M2M token error: {responseContent}");
                   tokenResponse.EnsureSuccessStatusCode();
               }
               
               var tokenObject = await tokenResponse.Content.ReadFromJsonAsync<Dictionary<string, object>>();
               if (tokenObject == null || !tokenObject.TryGetValue("access_token", out object? accessTokenValue))
               {
                    _logger.LogError("Could not find access_token in Auth0 M2M response.");
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
           catch (Exception ex)
           {
               _logger.LogError(ex, "Failed to get API client using M2M credentials");
               throw;
           }
       }
   }

}