
using Prestige.Api.Domain;

namespace Prestige.Api.Domain
{
    public class UserTrack
    {

        public int TotalTime { get; private set; }
        public Track Track { get; private set; }
        public User User { get; private set; }
        public bool IsFavorite { get; private set; }

        private UserTrack() { }

        public UserTrack(User user, int totalTime, Track track)
        {
            User = user;
            TotalTime = totalTime;
            Track = track;
            IsFavorite = false;
        }

        public void IncrementTotalTime(int time)
        {
            TotalTime += time;
        }

        public void ToggleIsFavorite()
        {
            IsFavorite = !IsFavorite;
        }
    }
}
