namespace Prestige.Api.Domain
{
    public class UserAlbum
    {

        public int TotalTime { get; private set; }
        public Album Album { get; private set; }
        public User User { get; private set; }
        public bool IsFavorite { get; private set; }
        public bool IsPinned { get; private set; }
        public decimal? PersonalRatingScore { get; private set; }
        public int? RatingPosition { get; private set; }
        public DateTime LastUpdatedAt { get; private set; }

        private UserAlbum() { }

        public UserAlbum(User user, int totalTime, Album album)
        {
            User = user;
            TotalTime = totalTime;
            Album = album;
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
