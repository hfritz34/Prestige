namespace Prestige.Api.Endpoints.Prestige.RequestResponse
{
    public class UserTrackRequest
    {
        public required string TrackId { get; set; }
        public int TotalTime { get; set; } = 0;
    }
}