namespace Prestige.Api.Endpoints.Prestige.RequestResponse
{
    public class UserAlbumRequest
    {
        public int TotalTime { get; set; } = 0;
        public required string AlbumId { get; set; }
    }
}