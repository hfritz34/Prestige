namespace Prestige.Api.Endpoints.Profile
{
    public class RecentlyPlayedResponse
    {
        public string TrackName { get; set; }
        public string ArtistName { get; set; }
        public string ImageUrl { get; set; }

        public string Id { get; set; }


        public RecentlyPlayedResponse(string trackName, string artistName, string imageUrl, string id)
        {
            TrackName = trackName;
            ArtistName = artistName;
            ImageUrl = imageUrl;
            Id = id;


        }
    }
}