using System.Security.Claims;
using Prestige.Api.Logging;
using Prestige.Api.Data;
using Prestige.Api.Endpoints.UserEndpoints.RequestResponse;
using System.Net.Http.Headers;
using System.Text.Json.Serialization;

namespace Prestige.Api.Endpoints
{
    public abstract class BaseService
    {
        private readonly PrestigeContext _prestigeDb;
        private readonly ClaimsPrincipal _principal;
        protected readonly ILogger<BaseService> _logger;
        private readonly IConfiguration _config;

        public PrestigeContext PrestigeDb => _prestigeDb;
        public ILogger<BaseService> Logger => _logger;
        public IConfiguration Config => _config;
        public ClaimsPrincipal Principal => _principal;




        public async Task<string> GetAccessToken(string id)
        {

            var user = PrestigeDb.Users.FirstOrDefault(u => u.Id == id) ?? throw Logger.UserNotFound(id);
            if (user.ExpiresAt < DateTime.Now.AddMinutes(-2))
            {
                var auth0User = await GetAuth0UserAsync();

                user.UpdateTokens(
                    auth0User.Identities.FirstOrDefault()?.AccessToken ?? throw Logger.ConfigurationMissing("Access Token"),
                    auth0User.Identities.FirstOrDefault()?.RefreshToken ?? throw Logger.ConfigurationMissing("Refresh Token"),
                    DateTime.Now.AddMinutes(5)
                );
            }


            PrestigeDb.SaveChanges();
            return user.AccessToken;
        }

        public async Task<string> GetAccessTokenAsync(string id)
        {
            var user = PrestigeDb.Users.FirstOrDefault(u => u.Id == id) ?? throw Logger.UserNotFound(id);

            if (user.ExpiresAt < DateTime.Now.AddMinutes(-2))
            {
                try
                {
                    // All Spotify users are managed through Auth0, so we'll use the Auth0 management API
                    var auth0User = await GetAuth0UserAsync();
                    var spotifyIdentity = auth0User.Identities.FirstOrDefault();
                    
                    if (spotifyIdentity == null)
                    {
                        throw new Exception("No Spotify identity found in Auth0 user profile");
                    }

                    user.UpdateTokens(
                        spotifyIdentity.AccessToken,
                        spotifyIdentity.RefreshToken,
                        DateTime.Now.AddHours(1)
                    );

                    PrestigeDb.SaveChanges();
                    Logger.LogInformation($"Refreshed Access Token for user {id} through Auth0");
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, $"Failed to refresh token for user {id}");
                    throw;
                }
            }

            Logger.LogInformation($"Returning Access Token for user {id}");
            return user.AccessToken;
        }

        public async Task<(string accessToken, string refreshToken)> RefreshSpotifyTokensAsync(string refreshToken)
        {
            var spotifyHttpClient = new HttpClient
            {
                BaseAddress = new Uri("https://accounts.spotify.com")
            };

            var tokenData = new Dictionary<string, string>
            {
                { "grant_type", "refresh_token" },
                { "refresh_token", refreshToken },
                { "client_id", Config["SPOTIFY_CLIENT_ID"] ?? throw new Exception("Spotify ClientId not found") },
                { "client_secret", Config["SPOTIFY_CLIENT_SECRET"] ?? throw new Exception("Spotify ClientSecret not found") }
            };

            var tokenRequest = new HttpRequestMessage(HttpMethod.Post, "/api/token")
            {
                Content = new FormUrlEncodedContent(tokenData)
            };
            tokenRequest.Content.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded");

            _logger.LogInformation("Sending token refresh request to Spotify");
            var tokenResponse = await spotifyHttpClient.SendAsync(tokenRequest);
            var responseContent = await tokenResponse.Content.ReadAsStringAsync();
            
            if (!tokenResponse.IsSuccessStatusCode)
            {
                _logger.LogError($"Failed to refresh Spotify token. Status: {tokenResponse.StatusCode}, Content: {responseContent}");
                throw new Exception($"Failed to refresh Spotify token: {tokenResponse.StatusCode} - {responseContent}");
            }

            var tokenContent = await tokenResponse.Content.ReadFromJsonAsync<TokenResponse>();

            if (tokenContent == null || string.IsNullOrEmpty(tokenContent.AccessToken))
            {
                _logger.LogError($"Invalid token response from Spotify: {responseContent}");
                throw new Exception("Failed to refresh Spotify access token: Invalid response format");
            }

            _logger.LogInformation("Successfully refreshed Spotify tokens");
            return (tokenContent.AccessToken, tokenContent.RefreshToken ?? refreshToken);
        }

        private async Task<Auth0UserResponse> GetAuth0UserAsync()
        {
            try 
            {
                var UserAuthId = Principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                _logger.LogInformation($"[Debug] Starting Auth0 token refresh for user ID: {UserAuthId}");
                if (UserAuthId == null)
                {
                    _logger.LogError("[Debug] NameIdentifier claim not found in Principal");
                    throw Logger.UserNotFound("AUTH");
                }

                var auth0Domain = Config.GetSection("Auth0:Domain").Value;
                var clientId = Config["Auth0:ClientId"];
                var audience = Config["Auth0:ManagementApiAudience"];

                _logger.LogInformation($"[Debug] Auth0 Configuration:" + 
                    $"\n  - Domain: {auth0Domain}" +
                    $"\n  - Client ID: {(clientId?.Length > 4 ? clientId.Substring(0, 4) + "..." : "null")}" +
                    $"\n  - Audience: {audience}");

                if (auth0Domain == null) throw Logger.ConfigurationMissing("Auth0:Domain");
                if (clientId == null) throw Logger.ConfigurationMissing("Auth0:ClientId");
                if (audience == null) throw Logger.ConfigurationMissing("Auth0:ManagementApiAudience");

                // Format domain
                if (!auth0Domain.StartsWith("https://"))
                {
                    auth0Domain = $"https://{auth0Domain}";
                    _logger.LogInformation($"[Debug] Added https:// to domain: {auth0Domain}");
                }
                if (!auth0Domain.EndsWith("/"))
                {
                    auth0Domain = $"{auth0Domain}/";
                    _logger.LogInformation($"[Debug] Added trailing slash to domain: {auth0Domain}");
                }

                _logger.LogInformation($"[Debug] Creating HttpClient with base address: {auth0Domain}");
                var client = new HttpClient(new HttpClientHandler())
                {
                    BaseAddress = new Uri(auth0Domain),
                };

                var tokenData = new Dictionary<string, string>
                {
                    { "client_id", clientId },
                    { "client_secret", Config["Auth0:ClientSecret"] ?? throw Logger.ConfigurationMissing("Auth0:ClientSecret") },
                    { "audience", audience },
                    { "grant_type", "client_credentials" }
                };

                _logger.LogInformation("[Debug] Preparing token request:" +
                    "\n  - Endpoint: oauth/token" +
                    "\n  - Grant Type: client_credentials" +
                    $"\n  - Audience: {audience}");

                var tokenRequest = new FormUrlEncodedContent(tokenData);
                _logger.LogInformation("[Debug] Sending token request to Auth0");
                var tokenResponse = await client.PostAsync("oauth/token", tokenRequest);
                
                var responseContent = await tokenResponse.Content.ReadAsStringAsync();
                _logger.LogInformation($"[Debug] Token response status: {tokenResponse.StatusCode}");
                
                if (!tokenResponse.IsSuccessStatusCode)
                {
                    _logger.LogError($"[Debug] Token request failed:" +
                        $"\n  - Status: {tokenResponse.StatusCode}" +
                        $"\n  - Content: {responseContent}" +
                        $"\n  - Headers: {string.Join(", ", tokenResponse.Headers.Select(h => $"{h.Key}={string.Join(",", h.Value)}"))}");
                    throw new Exception($"Failed to get Auth0 token: {tokenResponse.StatusCode} - {responseContent}");
                }

                var tokenContent = await tokenResponse.Content.ReadFromJsonAsync<Dictionary<string, object>>();
                _logger.LogInformation("[Debug] Successfully parsed token response");

                if (tokenContent == null)
                {
                    _logger.LogError("[Debug] Token response was null after JSON parsing");
                    throw Logger.TokenNotFound("Access Token");
                }

                if (!tokenContent.TryGetValue("access_token", out object? value))
                {
                    _logger.LogError($"[Debug] No access_token in response. Available keys: {string.Join(", ", tokenContent.Keys)}");
                    throw Logger.TokenNotFound("Access Token");
                }

                var accessToken = value.ToString();
                _logger.LogInformation($"[Debug] Got management API token. First 10 chars: {accessToken?.Substring(0, Math.Min(10, accessToken?.Length ?? 0))}...");

                var userEndpoint = $"api/v2/users/{UserAuthId}";
                _logger.LogInformation($"[Debug] Preparing user request to endpoint: {userEndpoint}");
                
                var userRequest = new HttpRequestMessage(HttpMethod.Get, userEndpoint);
                userRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

                _logger.LogInformation("[Debug] Sending user profile request");
                var userResponse = await client.SendAsync(userRequest);
                var userResponseContent = await userResponse.Content.ReadAsStringAsync();
                
                _logger.LogInformation($"[Debug] User profile response status: {userResponse.StatusCode}");
                
                if (!userResponse.IsSuccessStatusCode)
                {
                    _logger.LogError($"[Debug] User profile request failed:" +
                        $"\n  - Status: {userResponse.StatusCode}" +
                        $"\n  - Content: {userResponseContent}" +
                        $"\n  - Headers: {string.Join(", ", userResponse.Headers.Select(h => $"{h.Key}={string.Join(",", h.Value)}"))}");
                    throw new Exception($"Failed to get Auth0 user: {userResponse.StatusCode} - {userResponseContent}");
                }

                var auth0User = await userResponse.Content.ReadFromJsonAsync<Auth0UserResponse>();
                if (auth0User == null)
                {
                    _logger.LogError("[Debug] Auth0 user response was null after JSON parsing");
                    throw Logger.UserNotFound(UserAuthId);
                }

                _logger.LogInformation($"[Debug] Successfully retrieved Auth0 user profile:" +
                    $"\n  - User ID: {auth0User.UserId}" +
                    $"\n  - Email: {auth0User.Email}" +
                    $"\n  - Number of identities: {auth0User.Identities?.Count ?? 0}");

                return auth0User;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Debug] Exception in GetAuth0UserAsync");
                throw;
            }
        }

        public BaseService(PrestigeContext prestigeDb, ILogger<BaseService> logger, ClaimsPrincipal principal, IConfiguration config)
        {
            _prestigeDb = prestigeDb;
            _logger = logger;
            _principal = principal;
            _config = config;
        }
    }

    public class TokenResponse
    {
        [JsonPropertyName("access_token")]
        public string AccessToken { get; set; }

        [JsonPropertyName("refresh_token")]
        public string RefreshToken { get; set; }

        [JsonPropertyName("expires_in")]
        public int ExpiresIn { get; set; }

        [JsonPropertyName("token_type")]
        public string TokenType { get; set; }
    }
}
