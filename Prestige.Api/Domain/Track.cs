using Microsoft.EntityFrameworkCore.Update.Internal;
using Prestige.Api.Endpoints.Spotify.RequestResponse;

namespace Prestige.Api.Domain
{
        public class Track
        {
                public string Id { get; private set; }
                public string Name { get; private set; }
                public IEnumerable<Artist> Artists { get; private set; }
                public Album Album { get; private set; }
                public int DurationMs { get; private set; }

                public Track()
                {
                }
                public Track(TrackResponse trackResponse, Album album, IEnumerable<Artist> artists)
                {
                        Id = trackResponse.Id;
                        Name = trackResponse.Name;
                        DurationMs = trackResponse.DurationMs;
                        Album = album;
                        Artists = artists;
                }

                public void Update(TrackResponse trackResponse)
                {
                        Name = trackResponse.Name;
                        DurationMs = trackResponse.DurationMs;
                }
        }
}