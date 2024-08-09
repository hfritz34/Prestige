namespace Prestige.Api.Endpoints.Spotify.RequestResponse
{
    public class SearchRequest
    {
        public required string Query { get; set; }
        public int Limit { get; set; } = 20;
        public string Market { get; set; } = "US";
    }
}
