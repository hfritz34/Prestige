namespace Prestige.Api.Domain
{
    public class UserArtist
    {

        public int TotalTime { get; private set; }
        public Artist Artist { get; private set; }
        public User User { get; private set; }
        public bool IsFavorite { get; private set; }

        private UserArtist() { }

        public UserArtist(User user, int totalTime, Artist artist)
        {
            User = user;
            TotalTime = totalTime;
            Artist = artist;
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
