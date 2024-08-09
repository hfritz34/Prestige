using System.Text.Json.Serialization;

namespace Prestige.Api.Endpoints.Spotify.RequestResponse
{

    public class TrackResponse
    {
        public string Id { get; set; }
        public string Name { get; set; }

        [JsonPropertyName("duration_ms")]
        public int DurationMs { get; set; }
        public AlbumResponse Album { get; set; }
        public IEnumerable<ArtistResponse> Artists { get; set; }

        public TrackResponse()
        {
        }
        public TrackResponse(string id, string name, int durationMs, AlbumResponse album, IEnumerable<ArtistResponse> artists)
        {
            Id = id;
            Name = name;
            DurationMs = durationMs;
            Album = album;
            Artists = artists;
        }

        public TrackResponse(Domain.Track track)
        {
            Id = track.Id;
            Name = track.Name;
            DurationMs = track.DurationMs;
            Album = new AlbumResponse(track.Album);
            Artists = track.Artists.Select(artist => new ArtistResponse(artist)).ToList();
        }
    }

}