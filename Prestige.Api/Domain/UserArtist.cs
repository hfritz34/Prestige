namespace Prestige.Api.Domain
{
    public class UserArtist
    {

        public int TotalTime { get; private set; }
        public Artist Artist { get; private set; }
        public User User { get; private set; }
        public bool IsFavorite { get; private set; }
        public bool IsPinned { get; private set; }
        public decimal? PersonalRatingScore { get; private set; }
        public int? RatingPosition { get; private set; }
        public DateTime LastUpdatedAt { get; private set; }

        private UserArtist() { }

        public UserArtist(User user, int totalTime, Artist artist)
        {
            User = user;
            TotalTime = totalTime;
            Artist = artist;
            IsFavorite = false;
            IsPinned = false;
            LastUpdatedAt = DateTime.UtcNow;
        }

        public void IncrementTotalTime(int time)
        {
            TotalTime += time;
            LastUpdatedAt = DateTime.UtcNow;
        }

        public void ToggleIsFavorite()
        {
            IsFavorite = !IsFavorite;
        }

        public void ToggleIsPinned()
        {
            IsPinned = !IsPinned;
        }

        public void UpdateRating(decimal score, int position)
        {
            PersonalRatingScore = score;
            RatingPosition = position;
        }
    }
}
