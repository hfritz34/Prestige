using Prestige.Api.Domain;
using Prestige.Api.Endpoints.Spotify.RequestResponse;

namespace Prestige.Api.Endpoints.Prestige.RequestResponse
{
    public class UserTrackResponse
    {
        public int TotalTime { get; set; }
        public required TrackResponse Track { get; set; }
        public required string UserId { get; set; }

    }
}
