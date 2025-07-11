using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

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
               
               foreach (var doc in data)
               {
                   _logger.LogInformation($"Processing document - ID: {doc.id}, Track: {doc.trackId}, User: {doc.userId}");
               }
               
               var idsList = data.Select(d => d.trackId).ToList();
               var ids = string.Join(",", idsList);
               
               _logger.LogInformation($"Requesting tracks from API: {ids}");
               var idsResponse = await client.GetAsync($"spotify/tracks?ids={ids}");
               var responseContent = await idsResponse.Content.ReadAsStringAsync();
               
               if (!idsResponse.IsSuccessStatusCode)
               {
                   _logger.LogError($"Track request failed: {idsResponse.StatusCode} - {responseContent}");
                   idsResponse.EnsureSuccessStatusCode();
               }

               foreach (var doc in data)
               {
                   try
                   {
                       _logger.LogInformation($"Posting track {doc.trackId} for user {doc.userId}");
                       var userTrackResponse = await client.PostAsJsonAsync($"prestige/{doc.userId}/tracks", new
                       {
                           TrackId = doc.trackId,
                           TotalTime = doc.duration_ms / 1000
                       });

                       if (!userTrackResponse.IsSuccessStatusCode)
                       {
                           var errorContent = await userTrackResponse.Content.ReadAsStringAsync();
                           _logger.LogError($"Failed posting track: {userTrackResponse.StatusCode} - {errorContent}");
                           userTrackResponse.EnsureSuccessStatusCode();
                       }
                       
                       _logger.LogInformation($"Successfully processed track {doc.trackId}");
                   }
                   catch (Exception ex)
                   {
                       _logger.LogError(ex, $"Error processing track {doc.trackId} for user {doc.userId}");
                       throw;
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