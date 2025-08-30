using Prestige.Api.Endpoints.Prestige.RequestResponse;
using Prestige.Api.Endpoints.Profile;
using Prestige.Api.Domain;

namespace Prestige.Api.Endpoints.FriendshipEndpoints.RequestResponse
{
    public class FriendResponse
    {
        public string Id { get; set; }
        public string Nickname { get; set; }
        public string ProfilePicUrl { get; set; }
        public string Name { get; set; }
        public FriendRequestStatus Status { get; set; } = FriendRequestStatus.Accepted;
        public DateTime? RequestDate { get; set; }
        public DateTime? AcceptedDate { get; set; }
        public List<UserTrackResponse> FavoriteTracks { get; set; } = new List<UserTrackResponse>();
        public List<UserAlbumResponse> FavoriteAlbums { get; set; } = new List<UserAlbumResponse>();
        public List<UserArtistResponse> FavoriteArtists { get; set; } = new List<UserArtistResponse>();
        public List<UserTrackResponse> TopTracks { get; set; } = new List<UserTrackResponse>();
        public List<UserAlbumResponse> TopAlbums { get; set; } = new List<UserAlbumResponse>();
        public List<UserArtistResponse> TopArtists { get; set; } = new List<UserArtistResponse>();
    }

    public class ItemComparisonResponse
    {
        public UserStats UserStats { get; set; }
        public UserStats FriendStats { get; set; }
        public string ItemId { get; set; }
        public string ItemType { get; set; }
        public string ItemName { get; set; }
        public string ItemImageUrl { get; set; }
        public string FriendId { get; set; }
        public string FriendNickname { get; set; }
    }

    public class UserStats
    {
        public int? ListeningTime { get; set; }
        public double? RatingScore { get; set; }
        public string PrestigeTier { get; set; }
        public int? Position { get; set; }
    }
}
