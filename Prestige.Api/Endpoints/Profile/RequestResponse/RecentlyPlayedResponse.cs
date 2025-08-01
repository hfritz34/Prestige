namespace Prestige.Api.Endpoints.Profile
{
    public class RecentlyPlayedResponse
    {
        public string TrackName { get; set; }
        public string ArtistName { get; set; }
        public string ImageUrl { get; set; }
        public string Id { get; set; }

        public RecentlyPlayedResponse(string trackName, string artistName, string imageUrl, string id)
        {
            TrackName = trackName;
            ArtistName = artistName;
            ImageUrl = imageUrl;
            Id = id;
        }
    }

    public class RecentlyPlayedAlbumResponse
    {
        public string AlbumName { get; set; }
        public string ArtistName { get; set; }
        public string ImageUrl { get; set; }
        public string Id { get; set; }

        public RecentlyPlayedAlbumResponse(string albumName, string artistName, string imageUrl, string id)
        {
            AlbumName = albumName;
            ArtistName = artistName;
            ImageUrl = imageUrl;
            Id = id;
        }
    }

    public class RecentlyPlayedArtistResponse
    {
        public string ArtistName { get; set; }
        public string ImageUrl { get; set; }
        public string Id { get; set; }

        public RecentlyPlayedArtistResponse(string artistName, string imageUrl, string id)
        {
            ArtistName = artistName;
            ImageUrl = imageUrl;
            Id = id;
        }
    }
}