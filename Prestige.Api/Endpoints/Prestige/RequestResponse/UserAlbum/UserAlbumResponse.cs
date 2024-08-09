using Prestige.Api.Domain;
using Prestige.Api.Endpoints.Spotify.RequestResponse;

namespace Prestige.Api.Endpoints.Prestige.RequestResponse
{
    public class UserAlbumResponse
    {
        public int TotalTime { get; set; }
        public required AlbumResponse Album { get; set; }
        public required string UserId { get; set; }

    }
}