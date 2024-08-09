namespace Prestige.Api.Endpoints.Prestige.RequestResponse
{
    public class UserArtistRequest
    {
        public int TotalTime { get; set; } = 0;
        public required string ArtistId { get; set; }
    }
}