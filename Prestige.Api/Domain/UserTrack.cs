
using Prestige.Api.Domain;

namespace Prestige.Api.Domain
{
    public class UserTrack
    {

        public int TotalTime { get; private set; }
        public Track Track { get; private set; }
        public User User { get; private set; }
        public bool IsFavorite { get; private set; }
        public bool IsPinned { get; private set; }
        public decimal? PersonalRatingScore { get; private set; }
        public int? RatingPosition { get; private set; }

        private UserTrack() { }

        public UserTrack(User user, int totalTime, Track track)
        {
            User = user;
            TotalTime = totalTime;
            Track = track;
            IsFavorite = false;
            IsPinned = false;
        }

        public void IncrementTotalTime(int time)
        {
            TotalTime += time;
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
