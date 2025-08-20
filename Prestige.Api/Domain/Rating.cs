namespace Prestige.Api.Domain
{
    public class Rating
    {
        public int Id { get; private set; }
        public User User { get; private set; }
        public string ItemId { get; private set; }
        public string ItemType { get; private set; } // "Album", "Artist", "Track"
        public string? AlbumId { get; private set; } // For tracks, stores the album ID for comparison filtering
        public RatingCategory Category { get; private set; }
        public int Position { get; private set; }
        public int? RankWithinAlbum { get; private set; } // For tracks only - positional rank within album (1st, 2nd, 3rd)
        public decimal PersonalScore { get; private set; }
        public DateTime CreatedAt { get; private set; }
        public DateTime UpdatedAt { get; private set; }

        private Rating() { }

        public Rating(User user, string itemId, string itemType, RatingCategory category, string? albumId = null)
        {
            User = user;
            ItemId = itemId;
            ItemType = itemType;
            AlbumId = albumId;
            Category = category;
            Position = 0;
            PersonalScore = 0;
            CreatedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
        }

        public Rating(User user, string itemId, string itemType, RatingCategory category, int position, decimal personalScore, string? albumId = null)
        {
            User = user;
            ItemId = itemId;
            ItemType = itemType;
            AlbumId = albumId;
            Category = category;
            Position = position;
            PersonalScore = personalScore;
            CreatedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
        }

        public void UpdatePosition(int position)
        {
            Position = position;
            UpdatedAt = DateTime.UtcNow;
        }

        public void UpdateRankWithinAlbum(int? rank)
        {
            RankWithinAlbum = rank;
            UpdatedAt = DateTime.UtcNow;
        }

        public void EnsureAlbumId(string? albumId)
        {
            if (!string.IsNullOrEmpty(albumId) && string.IsNullOrEmpty(AlbumId))
            {
                AlbumId = albumId;
                UpdatedAt = DateTime.UtcNow;
            }
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

        public void UpdateRating(decimal personalScore, RatingCategory category, int position)
        {
            PersonalScore = personalScore;
            Category = category;
            Position = position;
            UpdatedAt = DateTime.UtcNow;
        }
    }
}