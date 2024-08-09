using Prestige.Api.Domain;
using Prestige.Api.Endpoints.Spotify.RequestResponse;

namespace Prestige.Api.Endpoints.Prestige.RequestResponse
{
    public class UserArtistResponse
    {
        public int TotalTime { get; set; }
        public required ArtistResponse Artist { get; set; }
        public required string UserId { get;  set; }

    }
}