using Prestige.Api.Endpoints.Spotify.RequestResponse;

namespace Prestige.Api.Domain
{
    public class Artist
    {
        public string Id { get; private set; }
        public string Name { get; private set; }
        public IEnumerable<Image> Images { get; private set; }

        public Artist()
        {
        }
        public Artist(string id, string name, IEnumerable<Image> images)
        {
            Id = id;
            Name = name;
            Images = images;
        }
        public Artist(ArtistResponse artistResponse, IEnumerable<Image> images)
        {
            Id = artistResponse.Id;
            Name = artistResponse.Name;
            Images = images;
        }

        public void Update(ArtistResponse artistResponse , IEnumerable<Image> images)
        {
            Id = artistResponse.Id;
            Name = artistResponse.Name;
            Images = images;
        }
    }
}