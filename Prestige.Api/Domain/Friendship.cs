using System;

namespace Prestige.Api.Domain
{
    public class Friendship
    {
        public string UserId { get; set; }
        public User User { get; set; }

        public string FriendId { get; set; }
        public User Friend { get; set; }

    }
}
