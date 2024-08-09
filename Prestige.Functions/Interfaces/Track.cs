namespace Prestige.Functions.Interfaces
{
    public class Track : TrackSimplified
    {
        public AlbumSimplified Album { get; set; }
        public ExternalIds ExternalIds { get; set; }
        public int Popularity { get; set; }
    }
}
