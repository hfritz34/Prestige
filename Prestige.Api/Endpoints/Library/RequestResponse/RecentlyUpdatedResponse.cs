using Prestige.Api.Endpoints.Prestige.RequestResponse;

namespace Prestige.Api.Endpoints.Library.RequestResponse
{
    public class RecentlyUpdatedResponse
    {
        public List<UserTrackResponse> Tracks { get; set; } = new();
        public List<UserAlbumResponse> Albums { get; set; } = new();
        public List<UserArtistResponse> Artists { get; set; } = new();
    }
}