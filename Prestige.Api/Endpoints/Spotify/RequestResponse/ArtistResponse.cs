using System.Text.Json.Serialization;

namespace Prestige.Api.Endpoints.Spotify.RequestResponse
{
    public class ArtistResponse
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public IEnumerable<ImageResponse> Images { get; set; }

        [JsonConstructor]
        public ArtistResponse(string id, string name, IEnumerable<ImageResponse> images)
        {
            Id = id;
            Name = name;
            Images = images ?? [];
        }

        public ArtistResponse(Domain.Artist artist)
        {
            Id = artist.Id;
            Name = artist.Name;
            Images = artist.Images?.Select(image => new ImageResponse(image)).ToList() ?? [];
        }
    }
}