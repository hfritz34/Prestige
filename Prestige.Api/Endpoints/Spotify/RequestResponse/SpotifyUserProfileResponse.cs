using System.Text.Json.Serialization;

namespace Prestige.Api.Endpoints.Spotify.RequestResponse
{
    public class SpotifyUserProfileResponse
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("display_name")]
        public string? DisplayName { get; set; }

        [JsonPropertyName("email")]
        public string? Email { get; set; }

        [JsonPropertyName("images")]
        public List<ImageResponse> Images { get; set; } = new List<ImageResponse>();

        [JsonPropertyName("country")]
        public string? Country { get; set; }

        [JsonPropertyName("followers")]
        public SpotifyFollowersResponse? Followers { get; set; }

        [JsonPropertyName("product")]
        public string? Product { get; set; }

        [JsonPropertyName("explicit_content")]
        public SpotifyExplicitContentResponse? ExplicitContent { get; set; }

        [JsonPropertyName("external_urls")]
        public Dictionary<string, string> ExternalUrls { get; set; } = new Dictionary<string, string>();
    }

    public class SpotifyFollowersResponse
    {
        [JsonPropertyName("total")]
        public int Total { get; set; }
    }

    public class SpotifyExplicitContentResponse
    {
        [JsonPropertyName("filter_enabled")]
        public bool FilterEnabled { get; set; }

        [JsonPropertyName("filter_locked")]
        public bool FilterLocked { get; set; }
    }
}