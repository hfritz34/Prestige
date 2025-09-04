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

    public class FriendItemDetailsResponse
    {
        public string ItemId { get; set; }
        public string ItemType { get; set; }
        public string ItemName { get; set; }
        public string ItemImageUrl { get; set; }
        public string FriendId { get; set; }
        public string FriendNickname { get; set; }
        public int? FriendListeningTime { get; set; }
        public double? FriendRatingScore { get; set; }
        public int? FriendPosition { get; set; }
        public string FriendPrestigeTier { get; set; }
        public int? FriendRankWithinAlbum { get; set; }
        public bool IsPinned { get; set; }
        public bool IsFavorite { get; set; }
        public object AdditionalData { get; set; }
    }

    public class FriendTrackRankingResponse
    {
        public string TrackId { get; set; }
        public string TrackName { get; set; }
        public string TrackImageUrl { get; set; }
        public int TrackNumber { get; set; }
        public int Duration { get; set; }
        public string FriendId { get; set; }
        public int? FriendListeningTime { get; set; }
        public double? FriendRatingScore { get; set; }
        public int? FriendPosition { get; set; }
        public int? FriendRankWithinAlbum { get; set; }
        public string FriendPrestigeTier { get; set; }
    }

    public class FriendAlbumRatingResponse
    {
        public string AlbumId { get; set; }
        public string AlbumName { get; set; }
        public string AlbumImageUrl { get; set; }
        public DateTime ReleaseDate { get; set; }
        public int TrackCount { get; set; }
        public string FriendId { get; set; }
        public int? FriendListeningTime { get; set; }
        public double? FriendRatingScore { get; set; }
        public int? FriendPosition { get; set; }
        public string FriendPrestigeTier { get; set; }
        public bool IsPinned { get; set; }
        public bool IsFavorite { get; set; }
    }
}
