namespace Prestige.Api.Domain
{
    public class UserAlbum
    {

        public int TotalTime { get; private set; }
        public Album Album { get; private set; }
        public User User { get; private set; }
        public bool IsFavorite { get; private set; }

        private UserAlbum() { }

        public UserAlbum(User user, int totalTime, Album album)
        {
            User = user;
            TotalTime = totalTime;
            Album = album;
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
