using System.Text.Json.Serialization;

namespace Prestige.Api.Endpoints.UserEndpoints.RequestResponse
{
    public class SpotifyTokenResponse
    {
        [JsonPropertyName("access_token")]
        public required string AccessToken { get; set; }

        [JsonPropertyName("refresh_token")]
        public required string RefreshToken { get; set; }

        [JsonPropertyName("expires_in")]
        public int ExpiresIn { get; set; }
    }
}