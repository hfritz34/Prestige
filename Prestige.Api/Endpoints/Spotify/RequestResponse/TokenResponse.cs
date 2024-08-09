using System.Text.Json.Serialization;

namespace Prestige.Api.Endpoints.Spotify.RequestResponse
{
    public class TokenResponse
    {
        [JsonPropertyName("access_token")]
        public required string AccessToken { get; set; }

        [JsonPropertyName("expires_in")]
        public int ExpiresIn { get; set; }
    }
}