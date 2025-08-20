using System.Text.Json.Serialization;

namespace Prestige.Api.Endpoints.Spotify.RequestResponse
{
    public class SpotifyAlbumTracksResponse
    {
        [JsonPropertyName("items")]
        public List<SimplifiedTrackResponse> Items { get; set; } = new List<SimplifiedTrackResponse>();
        
        [JsonPropertyName("total")]
        public int Total { get; set; }
        
        [JsonPropertyName("limit")]
        public int Limit { get; set; }
        
        [JsonPropertyName("offset")]
        public int Offset { get; set; }
    }

    public class SimplifiedTrackResponse
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;
        
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;
        
        [JsonPropertyName("artists")]
        public List<ArtistResponse> Artists { get; set; } = new List<ArtistResponse>();
        
        [JsonPropertyName("duration_ms")]
        public int DurationMs { get; set; }
        
        [JsonPropertyName("track_number")]
        public int TrackNumber { get; set; }
        
        [JsonPropertyName("disc_number")]
        public int DiscNumber { get; set; }
    }
}