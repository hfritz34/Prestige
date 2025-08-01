namespace Prestige.Api.Domain
{
    public class RatingComparison
    {
        public int Id { get; private set; }
        public User User { get; private set; }
        public string ItemId1 { get; private set; }
        public string ItemId2 { get; private set; }
        public string ItemType { get; private set; } // "Album", "Artist", "Track"
        public string WinnerId { get; private set; }
        public DateTime ComparisonDate { get; private set; }

        private RatingComparison() { }

        public RatingComparison(User user, string itemId1, string itemId2, string itemType, string winnerId)
        {
            User = user;
            ItemId1 = itemId1;
            ItemId2 = itemId2;
            ItemType = itemType;
            WinnerId = winnerId;
            ComparisonDate = DateTime.UtcNow;
        }
    }
}