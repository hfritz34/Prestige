namespace Prestige.Functions.Interfaces
{
    public class AlbumSimplified
    {
        public string AlbumType { get; set; }
        public int TotalTracks { get; set; }
        public List<string> AvailableMarkets { get; set; }
        public ExternalUrls ExternalUrls { get; set; }
        public string Href { get; set; }
        public string Id { get; set; }
        public List<Image> Images { get; set; }
        public string Name { get; set; }
        public string ReleaseDate { get; set; }
        public string ReleaseDatePrecision { get; set; }
        public string Type { get; set; }
        public string Uri { get; set; }
        public List<ArtistSimplified> Artists { get; set; }
    }
}
