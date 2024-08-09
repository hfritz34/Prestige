using System.Text.Json.Serialization;
using Prestige.Api.Domain;

namespace Prestige.Api.Endpoints.Spotify.RequestResponse
{

    public class SearchResponse
    {
        public SearchTrackResponse? Tracks { get; set; }
        public SearchAlbumResponse? Albums { get; set; }
        public SearchArtistResponse? Artists { get; set; } 

    }

    public class SearchTrackResponse
    {
        public required IEnumerable<TrackResponse> Items { get; set; }
    }

    public class SearchAlbumResponse
    {
        public required IEnumerable<AlbumResponse> Items { get; set; }
    }

    public class SearchArtistResponse
    {
        public required IEnumerable<ArtistResponse> Items { get; set; }
    }
}