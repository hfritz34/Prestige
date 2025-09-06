using Prestige.Api.Domain;

namespace Prestige.Api.Endpoints.FriendshipEndpoints.RequestResponse
{
    public class FriendRequestResponse
    {
        public string Id { get; set; } = string.Empty;
        public string FromUserId { get; set; } = string.Empty;
        public string ToUserId { get; set; } = string.Empty;
        public string FromUserName { get; set; } = string.Empty;
        public string FromUserNickname { get; set; } = string.Empty;
        public string FromUserProfilePicUrl { get; set; } = string.Empty;
        public string ToUserName { get; set; } = string.Empty;
        public string ToUserNickname { get; set; } = string.Empty;
        public string ToUserProfilePicUrl { get; set; } = string.Empty;
        public FriendRequestStatus Status { get; set; }
        public DateTime RequestDate { get; set; }
        public DateTime? AcceptedDate { get; set; }
    }
}