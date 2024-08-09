using Prestige.Api.Endpoints.Prestige.RequestResponse;
using Prestige.Api.Endpoints.Profile;

namespace Prestige.Api.Endpoints.FriendshipEndpoints.RequestResponse
{
    public class FriendResponse
    {
        public string Id { get; set; }
        public string Nickname { get; set; }
        public string ProfilePicUrl { get; set; }
        public string Name { get; set; }
        public List<UserTrackResponse> FavoriteTracks { get; set; } = new List<UserTrackResponse>();
        public List<UserAlbumResponse> FavoriteAlbums { get; set; } = new List<UserAlbumResponse>();
        public List<UserArtistResponse> FavoriteArtists { get; set; } = new List<UserArtistResponse>();
        public List<UserTrackResponse> TopTracks { get; set; } = new List<UserTrackResponse>();
        public List<UserAlbumResponse> TopAlbums { get; set; } = new List<UserAlbumResponse>();
        public List<UserArtistResponse> TopArtists { get; set; } = new List<UserArtistResponse>();
    }
}
