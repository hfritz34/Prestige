using System.Text.Json.Serialization;
using Prestige.Api.Domain;

namespace Prestige.Api.Endpoints.Spotify.RequestResponse
{
    public class AlbumResponse
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public IEnumerable<ImageResponse> Images { get; set; }
        public IEnumerable<ArtistResponse> Artists { get; set; }

        [JsonConstructor]
        public AlbumResponse(string id, string name, IEnumerable<ImageResponse> images, IEnumerable<ArtistResponse> artists)
        {
            Id = id;
            Name = name;
            Images = images;
            Artists = artists;
        }

        public AlbumResponse(Album album)
        {
            Id = album.Id;
            Name = album.Name;
            Images = album.Images.Select(image => new ImageResponse(image)).ToList();
            Artists = album.Artists.Select(artist => new ArtistResponse(artist)).ToList();
        }
    }
}