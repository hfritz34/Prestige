using System;

namespace Prestige.Api.Domain
{
    public enum FriendRequestStatus
    {
        Pending = 0,
        Accepted = 1,
        Declined = 2
    }

    public class Friendship
    {
        public string UserId { get; set; }
        public User User { get; set; }

        public string FriendId { get; set; }
        public User Friend { get; set; }

        public FriendRequestStatus Status { get; set; } = FriendRequestStatus.Pending;
        public DateTime RequestDate { get; set; } = DateTime.UtcNow;
        public DateTime? AcceptedDate { get; set; }

    }
}
