using System;
using System.Data.SqlClient;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos.Linq;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Extensions.Abstractions;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

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
    CreateLeaseContainerIfNotExists = false)] IReadOnlyList<Document> data)
{
    try 
    {
        var client = await GetApiClientAsync();
        
        // Debug logging
        _logger.LogInformation($"Received {data.Count} documents from Cosmos DB");
        foreach (var doc in data)
        {
            _logger.LogInformation($"Document ID: {doc.id}, Track ID: {doc.trackId}");
        }
        
        // Use trackId instead of id
        var idsList = data.Select(d => d.trackId).ToList();
        var ids = string.Join(",", idsList);
        
        _logger.LogInformation($"Requesting tracks with IDs: {ids}");
        
        var idsResponse = await client.GetAsync($"spotify/tracks?ids={ids}");
        var responseContent = await idsResponse.Content.ReadAsStringAsync();
        
        if (!idsResponse.IsSuccessStatusCode)
        {
            _logger.LogError($"Failed to get tracks. Status: {idsResponse.StatusCode}, Content: {responseContent}");
            idsResponse.EnsureSuccessStatusCode();
        }

        if (data != null && data.Count > 0)
        {
            foreach (var json in data)
            {
                try
                {
                    _logger.LogInformation($"Processing track data for ID: {json.trackId}");
                    var userTrackResponse = await client.PostAsJsonAsync($"prestige/{json.userId}/tracks", new
                    {
                        TrackId = json.trackId,
                        TotalTime = json.duration_ms / 1000
                    });

                    var userTrackContent = await userTrackResponse.Content.ReadAsStringAsync();
                    if (!userTrackResponse.IsSuccessStatusCode)
                    {
                        _logger.LogError($"Failed to post track. Status: {userTrackResponse.StatusCode}, Content: {userTrackContent}");
                    }
                    userTrackResponse.EnsureSuccessStatusCode();

                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error processing track {json.trackId} for user {json.userId}");
                }
            }
        }
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error in CosmosDBParser");
        throw;
    }
}

private async Task<HttpClient> GetApiClientAsync()
{
    _logger.LogInformation("Starting Auth0 token request");
    var auth0Client = new HttpClient();
    var auth0Domain = Environment.GetEnvironmentVariable("Auth0Domain");
    
    _logger.LogInformation($"Requesting token from Auth0 domain: {auth0Domain}");
    
    var request = new HttpRequestMessage(HttpMethod.Post, $"https://{auth0Domain}/oauth/token");
    request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
    {
        ["grant_type"] = "client_credentials",
        ["client_id"] = Environment.GetEnvironmentVariable("Auth0ClientId"),
        ["client_secret"] = Environment.GetEnvironmentVariable("Auth0ClientSecret"),
        // Use the API audience instead of Management API audience
        ["audience"] = Environment.GetEnvironmentVariable("Auth0ApiAudience")
    });

    try 
    {
        var tokenResponse = await auth0Client.SendAsync(request);
        var responseContent = await tokenResponse.Content.ReadAsStringAsync();
        _logger.LogInformation($"Auth0 response status: {tokenResponse.StatusCode}");
        
        if (!tokenResponse.IsSuccessStatusCode)
        {
            _logger.LogError($"Auth0 error response: {responseContent}");
        }
        
        tokenResponse.EnsureSuccessStatusCode();
        
        var tokenObject = await tokenResponse.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        var accessToken = tokenObject["access_token"].ToString();

        var apiClient = new HttpClient();
        apiClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        apiClient.BaseAddress = new Uri(Environment.GetEnvironmentVariable("ApiBaseUrl"));

        _logger.LogInformation("Successfully created API client");
        return apiClient;
    }
    catch (Exception ex)
    {
        _logger.LogError($"Error getting API client: {ex.Message}");
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