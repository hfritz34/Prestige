
using Prestige.Api.Domain;


namespace Prestige.Api.Endpoints.Profile
{
    public class TopAlbumResponse
    {
        public string AlbumName { get; set; }
        public string ArtistName { get; set; }
        public double TotalTime { get; set; }
        public string ImageUrl { get; set; }

        public TopAlbumResponse(string albumName, string artistName, double totalTime, string imageUrl)
        {
            AlbumName = albumName;
            ArtistName = artistName;
            TotalTime = totalTime;
            ImageUrl = imageUrl;
        }
    }
}