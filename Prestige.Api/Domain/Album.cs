using Prestige.Api.Endpoints.Spotify.RequestResponse;

namespace Prestige.Api.Domain
{
        public class Album
        {
                public string Id { get; private set; }
                public string Name { get; private set; }
                public IEnumerable<Image> Images { get; set; }
                public IEnumerable<Artist> Artists { get; set; }

                public Album()
                {
                }

                public Album(string id, string name, IEnumerable<Image> images, IEnumerable<Artist> artist)
                {
                        Id = id;
                        Name = name;
                        Images = images;
                        Artists = artist;
                }
                public Album(AlbumResponse albumResponse, IEnumerable<Artist> artists , IEnumerable<Image> images)
                {
                        Id = albumResponse.Id;
                        Name = albumResponse.Name;
                        Images = images;
                        Artists = artists;
                }

                public void Update(AlbumResponse albumResponse, IEnumerable<Image> images)
                {
                        Name = albumResponse.Name;
                        Images = images;
                }
        }
}