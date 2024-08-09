
using Prestige.Api.Domain;


namespace Prestige.Api.Endpoints.Profile
{
    public class TopTrackResponse
    {
        public string TrackId { get; set; }
        public string TrackName { get; set; }
        public string AlbumName { get; set; }
        public string ArtistName { get; set; }
        public double TotalTime { get; set; }
        public string ImageUrl { get; set; }

        public TopTrackResponse(string trackId, string trackName, string albumName, string artistName, int totalTime, string imageUrl)
        {
            TrackId = trackId;
            TrackName = trackName;
            AlbumName = albumName;
            ArtistName = artistName;
            TotalTime = totalTime;
            ImageUrl = imageUrl;
        }
    }
}
