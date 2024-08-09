using System.Data.SqlClient;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace RecentlyPlayedTrigger
{
    public class RecentlyPlayedTrigger
    {
        private readonly ILogger _logger;
        private static readonly HttpClient httpClient = new HttpClient();
        private static readonly string? cosmosConnectionString = Environment.GetEnvironmentVariable("CosmosDBConnectionString");
        private static readonly string? sqlConnectionString = Environment.GetEnvironmentVariable("SqlConnectionString");
        private readonly CosmosClient cosmosClient;
        private readonly Database database;
        private readonly Container container;

        public RecentlyPlayedTrigger(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<RecentlyPlayedTrigger>();

            var cosmosClientOptions = new CosmosClientOptions
            {
                Serializer = new CosmosSystemTextJsonSerializer(new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    WriteIndented = true
                })
            };

            cosmosClient = new CosmosClient(cosmosConnectionString, cosmosClientOptions);
            database = cosmosClient.GetDatabase("MusicDB");
            container = database.GetContainer("RecentlyPlayed");
        }

        [Function("RecentlyPlayedTrigger")]
        public async Task Run([TimerTrigger("* * * * *")] TimerInfo myTimer)
        {
            _logger.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");

            if (myTimer.ScheduleStatus is not null)
            {
                _logger.LogInformation($"Next timer schedule at: {myTimer.ScheduleStatus.Next}");
            }

            try
            {
                var batchId = Guid.NewGuid().ToString();

                var users = await GetAllUsersAsync();

                foreach (var user in users)
                {
                    var (spotifyAccessToken, spotifyRefreshToken) = await GetSpotifyTokensAsync(user.Id);
                    _logger.LogInformation($"Spotify tokens fetched successfully for user: {user.Id}");

                    var recentlyPlayedTracksJson = await FetchRecentlyPlayedTracks(spotifyAccessToken, spotifyRefreshToken, user.Id);

                    if (recentlyPlayedTracksJson.GetProperty("items").GetArrayLength() > 0)
                    {
                        _logger.LogInformation($"Fetched recently played tracks for user: {user.Id}");
                        await StoreTracksInCosmosDB(user.Id, batchId, recentlyPlayedTracksJson);
                    }
                    else
                    {
                        _logger.LogInformation($"No recently played tracks found for user: {user.Id}");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while running the timer trigger function.");
            }
        }

        private async Task<JsonElement> FetchRecentlyPlayedTracks(string accessToken, string refreshToken, string userId)
        {
            string requestUrl = "https://api.spotify.com/v1/me/player/recently-played?limit=50";
            _logger.LogInformation($"Making request to Spotify API: {requestUrl}");

            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            var response = await httpClient.GetAsync(requestUrl);

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                _logger.LogWarning("Access token expired. Refreshing token...");
                (accessToken, refreshToken) = await RefreshSpotifyTokensAsync(refreshToken);

                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                response = await httpClient.GetAsync(requestUrl);
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError($"Failed to fetch recently played tracks. StatusCode: {response.StatusCode}, Content: {responseContent}");
                response.EnsureSuccessStatusCode();
            }

            //_logger.LogInformation($"Spotify API response: {responseContent}");
            return JsonDocument.Parse(responseContent).RootElement;
        }

        private async Task StoreTracksInCosmosDB(string userId, string batchId, JsonElement recentlyPlayedTracks)
        {
            var mostRecentPlayedAt = await GetMostRecentPlayedAt(userId);

            var items = recentlyPlayedTracks.GetProperty("items").EnumerateArray();

            foreach (var item in items)
            {
                if (item.TryGetProperty("played_at", out var playedAtElement) &&
                    item.TryGetProperty("track", out var trackElement))
                {
                    var playedAtString = playedAtElement.GetString();
                    if (DateTime.TryParse(playedAtString, out DateTime playedAt))
                    {
                        if (mostRecentPlayedAt == null || playedAt > mostRecentPlayedAt)
                        {
                            var trackId = trackElement.GetProperty("id").GetString();
                            var durationMs = trackElement.GetProperty("duration_ms").GetInt32();

                            var uniqueId = $"{trackId}_{playedAt:o}_{batchId}";

                            var trackData = new Dictionary<string, object>
                            {
                                { "id", uniqueId },
                                { "batchId", batchId },
                                { "userId", userId },
                                { "trackId", trackId },
                                { "duration_ms", durationMs },
                                { "played_at", playedAtString }
                            };

                            _logger.LogInformation($"Inserting track data for user {userId}: {JsonSerializer.Serialize(trackData)}");
                            await container.CreateItemAsync(trackData, new PartitionKey(userId));
                            _logger.LogInformation($"Track inserted for user {userId} with unique id: {uniqueId}, playedAt: {playedAt}");
                        }
                    }
                    else
                    {
                        _logger.LogWarning("Failed to parse played_at value: {PlayedAtString}", playedAtString);
                    }
                }
                else
                {
                    _logger.LogWarning("played_at or track property not found in item.");
                }
            }
        }

        private async Task<DateTime?> GetMostRecentPlayedAt(string userId)
        {
            var queryDefinition = new QueryDefinition("SELECT TOP 1 c.played_at FROM c WHERE c.userId = @userId ORDER BY c.played_at DESC")
                .WithParameter("@userId", userId);
            var queryIterator = container.GetItemQueryIterator<JsonElement>(queryDefinition, requestOptions: new QueryRequestOptions { PartitionKey = new PartitionKey(userId), MaxItemCount = 1 });

            while (queryIterator.HasMoreResults)
            {
                try
                {
                    var response = await queryIterator.ReadNextAsync();
                    _logger.LogInformation($"Query response: {response}");

                    if (response.Any())
                    {
                        foreach (var document in response)
                        {
                            if (document.ValueKind != JsonValueKind.Undefined && document.TryGetProperty("played_at", out var playedAtElement))
                            {
                                if (playedAtElement.ValueKind == JsonValueKind.String)
                                {
                                    var playedAtString = playedAtElement.GetString();
                                    if (DateTime.TryParse(playedAtString, out DateTime playedAt))
                                    {
                                        return playedAt;
                                    }
                                    else
                                    {
                                        _logger.LogWarning("Failed to parse played_at value: {PlayedAtString}", playedAtString);
                                    }
                                }
                                else
                                {
                                    _logger.LogWarning("The played_at property is not a string.");
                                }
                            }
                            else
                            {
                                _logger.LogWarning("played_at property not found in the response or document is undefined.");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "An error occurred while querying or parsing the most recent played_at value.");
                }
            }

            _logger.LogInformation("Cosmos DB container is empty or query returned no results.");
            return null;
        }

        private async Task<List<User>> GetAllUsersAsync()
        {
            var users = new List<User>();
            using (var connection = new SqlConnection(sqlConnectionString))
            {
                await connection.OpenAsync();
                var query = "SELECT Id FROM [User]";

                using (var command = new SqlCommand(query, connection))
                {
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            users.Add(new User
                            {
                                Id = reader.GetString(0)
                            });
                        }
                    }
                }
            }
            return users;
        }

        private async Task<(string accessToken, string refreshToken)> GetSpotifyTokensAsync(string userId)
        {
            var managementToken = await GetAuth0ManagementTokenAsync();
            return await GetUserTokensAsync(userId, managementToken);
        }

        private async Task<(string accessToken, string refreshToken)> GetUserTokensAsync(string userId, string managementToken)
        {
            var fullUserId = $"oauth2|Spotify|{userId}";

            var auth0Client = new HttpClient
            {
                BaseAddress = new Uri("https://dev-u10jtlqih3lq02fh.us.auth0.com")
            };

            auth0Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", managementToken);

            var response = await auth0Client.GetAsync($"/api/v2/users/{fullUserId}");
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadFromJsonAsync<JsonElement>();
            var identities = content.GetProperty("identities").EnumerateArray().First();
            var accessToken = identities.GetProperty("access_token").GetString();
            var refreshToken = identities.GetProperty("refresh_token").GetString();

            return (accessToken, refreshToken);
        }

        private async Task<(string accessToken, string refreshToken)> RefreshSpotifyTokensAsync(string refreshToken)
        {
            var spotifyHttpClient = new HttpClient
            {
                BaseAddress = new Uri("https://accounts.spotify.com")
            };

            var tokenData = new Dictionary<string, string>
            {
                { "grant_type", "refresh_token" },
                { "refresh_token", refreshToken },
                { "client_id", Environment.GetEnvironmentVariable("SpotifyClientId") ?? throw new Exception("Spotify ClientId not found") },
                { "client_secret", Environment.GetEnvironmentVariable("SpotifyClientSecret") ?? throw new Exception("Spotify ClientSecret not found") }
            };

            var tokenRequest = new HttpRequestMessage(HttpMethod.Post, "/api/token")
            {
                Content = new FormUrlEncodedContent(tokenData)
            };

            var tokenResponse = await spotifyHttpClient.SendAsync(tokenRequest);
            tokenResponse.EnsureSuccessStatusCode();

            var tokenContent = await tokenResponse.Content.ReadFromJsonAsync<JsonElement>();

            if (!tokenContent.TryGetProperty("access_token", out var accessTokenElement))
            {
                throw new Exception("Failed to refresh Spotify tokens.");
            }

            var newAccessToken = accessTokenElement.GetString();
            var newRefreshToken = tokenContent.TryGetProperty("refresh_token", out var refreshTokenElement) ? refreshTokenElement.GetString() : refreshToken;

            return (newAccessToken, newRefreshToken);
        }

        private async Task<string> GetAuth0ManagementTokenAsync()
        {
            var auth0Client = new HttpClient
            {
                BaseAddress = new Uri("https://dev-u10jtlqih3lq02fh.us.auth0.com")
            };

            var tokenData = new Dictionary<string, string>
            {
                { "grant_type", "client_credentials" },
                { "client_id", Environment.GetEnvironmentVariable("Auth0ClientId") ?? throw new Exception("Auth0ClientId not found") },
                { "client_secret", Environment.GetEnvironmentVariable("Auth0ClientSecret") ?? throw new Exception("Auth0ClientSecret not found") },
                { "audience", Environment.GetEnvironmentVariable("Auth0Audience") ?? throw new Exception("Auth0Audience not found") }
            };

            var tokenRequest = new HttpRequestMessage(HttpMethod.Post, "/oauth/token")
            {
                Content = new FormUrlEncodedContent(tokenData)
            };

            var tokenResponse = await auth0Client.SendAsync(tokenRequest);
            tokenResponse.EnsureSuccessStatusCode();

            var tokenContent = await tokenResponse.Content.ReadFromJsonAsync<JsonElement>();

            return tokenContent.GetProperty("access_token").GetString();
        }

        public class User
        {
            public string Id { get; set; }
        }
    }
}
