namespace Prestige.Api.Domain
{
    public class Rating
    {
        public int Id { get; private set; }
        public User User { get; private set; }
        public string ItemId { get; private set; }
        public string ItemType { get; private set; } // "Album", "Artist", "Track"
        public RatingCategory Category { get; private set; }
        public int Position { get; private set; }
        public decimal PersonalScore { get; private set; }
        public DateTime CreatedAt { get; private set; }
        public DateTime UpdatedAt { get; private set; }

        private Rating() { }

        public Rating(User user, string itemId, string itemType, RatingCategory category)
        {
            User = user;
            ItemId = itemId;
            ItemType = itemType;
            Category = category;
            Position = 0;
            PersonalScore = 0;
            CreatedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
        }

        public void UpdatePosition(int position)
        {
            Position = position;
            UpdatedAt = DateTime.UtcNow;
        }

        public void UpdateScore(decimal score)
        {
            PersonalScore = score;
            UpdatedAt = DateTime.UtcNow;
        }

        public void UpdateCategory(RatingCategory category)
        {
            Category = category;
            UpdatedAt = DateTime.UtcNow;
        }
    }
}