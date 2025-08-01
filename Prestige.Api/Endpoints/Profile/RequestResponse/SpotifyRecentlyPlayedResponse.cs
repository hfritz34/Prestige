using System.Text.Json.Serialization;

namespace Prestige.Api.Endpoints.Profile
{
    public class SpotifyRecentlyPlayedResponse
    {
        [JsonPropertyName("items")]
        public List<SpotifyPlayedItem> Items { get; set; }
    }

    public class SpotifyPlayedItem
    {
        [JsonPropertyName("track")]
        public SpotifyTrack Track { get; set; }
    }

    public class SpotifyTrack
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("artists")]
        public List<SpotifyArtist> Artists { get; set; }

        [JsonPropertyName("album")]
        public SpotifyAlbum Album { get; set; }
    }

    public class SpotifyArtist
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }
    }

    public class SpotifyAlbum
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("artists")]
        public List<SpotifyArtist> Artists { get; set; }

        [JsonPropertyName("images")]
        public List<SpotifyImage> Images { get; set; }
    }

    public class SpotifyImage
    {
        [JsonPropertyName("url")]
        public string Url { get; set; }
    }
}
