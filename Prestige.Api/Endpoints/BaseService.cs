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
        private readonly ILogger<BaseService> _logger;
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
                (string newAccessToken, string newRefreshToken) = await RefreshSpotifyTokensAsync(user.RefreshToken);

                user.UpdateTokens(newAccessToken, newRefreshToken, DateTime.Now.AddMinutes(60));
                PrestigeDb.SaveChanges();
                Logger.LogInformation($"Refreshed Access Token for user {id}: {newAccessToken}");

                return newAccessToken;
            }

            Logger.LogInformation($"Access Token for user {id}: {user.AccessToken}");
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

            var tokenResponse = await spotifyHttpClient.SendAsync(tokenRequest);
            tokenResponse.EnsureSuccessStatusCode();

            var tokenContent = await tokenResponse.Content.ReadFromJsonAsync<TokenResponse>();

            if (tokenContent == null || string.IsNullOrEmpty(tokenContent.AccessToken))
            {
                throw new Exception("Failed to refresh Spotify access token.");
            }

            return (tokenContent.AccessToken, tokenContent.RefreshToken ?? refreshToken);
        }

        private async Task<Auth0UserResponse> GetAuth0UserAsync()
        {
            var UserAuthId = Principal.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? throw Logger.UserNotFound("AUTH");
            var client = new HttpClient(new HttpClientHandler())
            {
                BaseAddress = new Uri(Config.GetSection("Auth0:Domain").Value ?? throw Logger.ConfigurationMissing("Auth0:Domain")),
            };

            var tokenData = new Dictionary<string, string>
            {
                { "client_id", Config.GetSection("Auth0:Client_Id").Value ??  throw Logger.ConfigurationMissing("Auth0:Client_Id") },
                { "client_secret", Config.GetSection("Auth0:Client_Secret").Value ?? throw Logger.ConfigurationMissing("Auth0:Client_Secret") },
                { "audience", "https://dev-u10jtlqih3lq02fh.us.auth0.com/api/v2/" },
                { "grant_type", "client_credentials" }
            };

            var tokenRequest = new FormUrlEncodedContent(tokenData);

            var tokenResponse = await client.PostAsync("/oauth/token", tokenRequest);
            tokenResponse.EnsureSuccessStatusCode();

            var tokenContent = await tokenResponse.Content.ReadFromJsonAsync<Dictionary<string, object>>();

            if (tokenContent == null || !tokenContent.TryGetValue("access_token", out object? value))
            {
                throw Logger.TokenNotFound("Access Token");
            }

            var accessToken = value.ToString();

            var userRequest = new HttpRequestMessage(HttpMethod.Get, "/api/v2/users/" + UserAuthId);
            userRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            var userResponse = await client.SendAsync(userRequest);
            userResponse.EnsureSuccessStatusCode();

            var auth0User = await userResponse.Content.ReadFromJsonAsync<Auth0UserResponse>() ?? throw Logger.UserNotFound(UserAuthId);

            return auth0User;
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
