using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Prestige.Functions.Models;

namespace CosmosDBParser
{
   public class CosmosDBParser
   {
       private readonly ILogger _logger;

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
           _logger.LogInformation($"CosmosDBParser triggered with {data.Count} documents");
           
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
                                   else
                                   {
                                       _logger.LogError($"Failed posting track {doc.trackId}: {userTrackResponse.StatusCode} - {errorContent}");
                                   }
                                   return false;
                               }
                               
                               return true;
                           }
                           catch (Exception ex)
                           {
                               _logger.LogError(ex, $"Error processing track {doc.trackId} for user {doc.userId}");
                               return false;
                           }
                       });
                       
                       // Wait for all requests in this batch to complete
                       var results = await Task.WhenAll(batchTasks);
                       var successCount = results.Count(r => r);
                       var failureCount = results.Count(r => !r);
                       
                       _logger.LogInformation($"Batch {batchNumber} completed: {successCount} successful, {failureCount} failed");
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
           _logger.LogInformation("Getting Auth0 M2M token for Prestige API");
           var auth0Client = new HttpClient();
           // Ensure domain has trailing slash for consistency
           var auth0Domain = Environment.GetEnvironmentVariable("Auth0Domain");
           if (!string.IsNullOrEmpty(auth0Domain) && !auth0Domain.EndsWith("/"))
           {
               auth0Domain += "/";
           }
           
           // Use dedicated settings for the M2M app calling the API
           var m2mClientId = Environment.GetEnvironmentVariable("FunctionM2MClientId") ?? throw new Exception("FunctionM2MClientId setting not found");
           var m2mClientSecret = Environment.GetEnvironmentVariable("FunctionM2MClientSecret") ?? throw new Exception("FunctionM2MClientSecret setting not found");
           // Use the API's audience, NOT the management API audience
           var apiAudience = Environment.GetEnvironmentVariable("Auth0ApiAudience") ?? throw new Exception("Auth0ApiAudience setting not found"); 

           var request = new HttpRequestMessage(HttpMethod.Post, $"https://{auth0Domain}oauth/token"); 
           request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
           {
               {"grant_type", "client_credentials"},
               {"client_id", m2mClientId},
               {"client_secret", m2mClientSecret},
               {"audience", apiAudience} // Use the API audience here
           });

           try 
           {
               var tokenResponse = await auth0Client.SendAsync(request);
               var responseContent = await tokenResponse.Content.ReadAsStringAsync();
               _logger.LogInformation($"Auth0 M2M token response: {tokenResponse.StatusCode}");
               
               if (!tokenResponse.IsSuccessStatusCode)
               {
                   _logger.LogError($"Auth0 M2M token error: {responseContent}");
                   tokenResponse.EnsureSuccessStatusCode(); // Throw exception if failed
               }
               
               var tokenObject = await tokenResponse.Content.ReadFromJsonAsync<Dictionary<string, object>>();
               if (tokenObject == null || !tokenObject.TryGetValue("access_token", out object? value))
               {
                    _logger.LogError("Could not find access_token in Auth0 M2M response.");
                    throw new Exception("Failed to retrieve access token from Auth0 M2M response.");
               }
               var accessToken = value.ToString();
               _logger.LogInformation("Successfully obtained M2M access token for Prestige API.");

               var apiClient = new HttpClient();
               apiClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
               // Use the API base URL configured
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