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
            var client = await GetApiClientAsync();
            var idsList = data.Select(d => d.id).ToList();
            var ids = string.Join(",", idsList);
            var idsResponse = await client.GetAsync($"spotify/tracks?ids={ids}");
            idsResponse.EnsureSuccessStatusCode();
            if (data != null && data.Count > 0)
            {
                foreach (var json in data)
                {
                    try
                    {
                        var id = json.id;
                        var batchId = json.batchId;
                        var userId = json.userId;
                        var trackId = json.trackId;
                        int duration_ms = json.duration_ms;
                        var played_at = json.played_at;
                        var duration_s = duration_ms / 1000;

                        _logger.LogInformation($"INFORMATION | Batch ID: {batchId} User ID: {userId} Track ID: {trackId} Duration(ms): {duration_ms} Played At: {played_at}");

                        var userTrackResponse = await client.PostAsJsonAsync($"prestige/{userId}/tracks", new
                        {
                            TrackId = trackId,
                            TotalTime = duration_s
                        });

                        userTrackResponse.EnsureSuccessStatusCode();

                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Exception encountered: {ex.Message}");
                    }
                }
            }
        }

        private async Task<HttpClient> GetApiClientAsync()
        {
            var auth0Client = new HttpClient();
            var request = new HttpRequestMessage(HttpMethod.Post, $"https://{Environment.GetEnvironmentVariable("Auth0Domain")}/oauth/token");
            request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "client_credentials",
                ["client_id"] = Environment.GetEnvironmentVariable("Auth0ClientId"),
                ["client_secret"] = Environment.GetEnvironmentVariable("Auth0ClientSecret"),
                ["audience"] = Environment.GetEnvironmentVariable("ApiBaseUrl")
            });

            var tokenResponse = await auth0Client.SendAsync(request);
            tokenResponse.EnsureSuccessStatusCode();
            var tokenObject = await tokenResponse.Content.ReadFromJsonAsync<Dictionary<string, object>>();

            var accessToken = tokenObject["access_token"].ToString();

            var apiClient = new HttpClient();

            apiClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            apiClient.BaseAddress = new Uri(Environment.GetEnvironmentVariable("ApiBaseUrl"));

            return apiClient;
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