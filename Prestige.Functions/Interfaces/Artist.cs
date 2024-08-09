namespace Prestige.Functions.Interfaces
{
    public class Artist : ArtistSimplified
    {
        public Followers Followers { get; set; }
        public List<string> Genres { get; set; }
        public List<Image> Images { get; set; }
        public int Popularity { get; set; }
    }
}
