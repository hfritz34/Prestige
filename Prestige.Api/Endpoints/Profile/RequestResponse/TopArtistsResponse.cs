namespace Prestige.Api.Endpoints.Profile
{
    public class TopArtistResponse
    {

        public int TotalTime { get; private set; }

        public string ArtistName { get; private set; }

        public string ImageUrl { get; private set; }



        public TopArtistResponse(string artistName, int totalTime, string imageUrl)
        {
            ArtistName = artistName;
            TotalTime = totalTime;
            ImageUrl = imageUrl;
        }





    }
}