using System.Security.Claims;
using Prestige.Api.Data;
using Prestige.Api.Domain;
using Prestige.Api.Endpoints.Prestige.RequestResponse;
using Prestige.Api.Exceptions;
using Prestige.Api.Endpoints.Spotify;
using Prestige.Api.Logging;
using Microsoft.EntityFrameworkCore;
using Prestige.Api.Endpoints.Spotify.RequestResponse;

namespace Prestige.Api.Endpoints.Prestige
{
    public class PrestigeServices : BaseService
    {
        private readonly SpotifyServices _spotifyServices;

        public PrestigeServices(PrestigeContext db, ILogger<PrestigeServices> logger, ClaimsPrincipal principal, IConfiguration config, SpotifyServices spotifyServices) : base(db, logger, principal, config)
        {
            _spotifyServices = spotifyServices;
        }

        public async Task<UserTrackResponse> PostUserTrack(string userId, UserTrackRequest request)
        {
            var userTrack = PrestigeDb.UserTracks
                .Include(ut => ut.Track)
                    .ThenInclude(t => t.Album)
                        .ThenInclude(a => a.Images)
                .Include(t => t.Track.Album.Artists)
                    .ThenInclude(a => a.Images)
                .Include(t => t.Track.Artists)
                    .ThenInclude(a => a.Images)
                .Include(ut => ut.User)
                .FirstOrDefault(userTrack => userTrack.User.Id == userId && userTrack.Track.Id == request.TrackId);
            if (userTrack != null)
            {
                userTrack.IncrementTotalTime(request.TotalTime);
                await PostUserAblum(userId, new UserAlbumRequest()
                {
                    AlbumId = userTrack.Track.Album.Id,
                    TotalTime = request.TotalTime
                }
                );
                await PostUserArtist(userId, new UserArtistRequest()
                {
                    ArtistId = userTrack.Track.Artists.First().Id ?? throw Logger.ArtistNotFound(userTrack.Track.Artists.First().Id),
                    TotalTime = request.TotalTime
                }
                );
                return new UserTrackResponse()
                {
                    TotalTime = userTrack.TotalTime,
                    Track = new TrackResponse(userTrack.Track),
                    UserId = userTrack.User.Id
                };
            }

            var user = PrestigeDb.Users.FirstOrDefault(user => user.Id == userId)
                ?? throw Logger.UserNotFound(userId);
            var track = PrestigeDb.Tracks
                .Include(track => track.Album)
                    .ThenInclude(album => album.Images)
                .Include(track => track.Artists)
                    .ThenInclude(artist => artist.Images)
                .Include(track => track.Album.Artists)
                    .ThenInclude(artist => artist.Images)
                .FirstOrDefault(track => track.Id == request.TrackId);
            
            if (track == null)
            {
                // Track doesn't exist, fetch from Spotify and create it
                var spotifyTrack = await _spotifyServices.GetTrackByIdAsync(request.TrackId);
                if (spotifyTrack == null)
                {
                    throw Logger.TrackNotFound(request.TrackId);
                }
                
                // Reload the track from database after it was created by SpotifyServices
                track = PrestigeDb.Tracks
                    .Include(track => track.Album)
                        .ThenInclude(album => album.Images)
                    .Include(track => track.Artists)
                        .ThenInclude(artist => artist.Images)
                    .Include(track => track.Album.Artists)
                        .ThenInclude(artist => artist.Images)
                    .FirstOrDefault(track => track.Id == request.TrackId)
                    ?? throw Logger.TrackNotFound(request.TrackId);
            }
            userTrack = new UserTrack(user, request.TotalTime, track);

            PrestigeDb.UserTracks.Add(userTrack);
            PrestigeDb.SaveChanges();
            userTrack = PrestigeDb.UserTracks
                .Include(ut => ut.Track)
                    .ThenInclude(t => t.Album)
                        .ThenInclude(a => a.Images)
                .Include(t => t.Track.Album.Artists)
                    .ThenInclude(a => a.Images)
                .Include(t => t.Track.Artists)
                    .ThenInclude(a => a.Images)
                .Include(ut => ut.User)
                .FirstOrDefault(userTrack => userTrack.User.Id == userId && userTrack.Track.Id == request.TrackId)
                ?? throw Logger.UserTrackNotFound(userId, request.TrackId);

            //Increment Album and Artist Total Time
            var postAlbumResponse = await PostUserAblum(userId, new UserAlbumRequest()
            {
                AlbumId = userTrack.Track.Album.Id,
                TotalTime = request.TotalTime
            }
            ) ?? throw Logger.UserAlbumNotFound(userId, userTrack.Track.Album.Id);
            var postArtistResponse = await PostUserArtist(userId, new UserArtistRequest()
            {
                ArtistId = userTrack.Track.Artists.First().Id ?? throw Logger.ArtistNotFound(userTrack.Track.Artists.First().Id),
                TotalTime = request.TotalTime
            }
            ) ?? throw Logger.UserArtistNotFound(userId, userTrack.Track.Artists.First().Id);
            PrestigeDb.SaveChanges();


            return new UserTrackResponse()
            {
                TotalTime = userTrack.TotalTime,
                Track = new TrackResponse(userTrack.Track),
                UserId = userTrack.User.Id
            };
        }

        public UserTrackResponse GetUserTrack(string userId, string trackId)
        {
            var userTrack = PrestigeDb.UserTracks
                .Include(ut => ut.Track)
                    .ThenInclude(t => t.Album)
                        .ThenInclude(a => a.Images)
                .Include(t => t.Track.Album.Artists)
                    .ThenInclude(a => a.Images)
                .Include(t => t.Track.Artists)
                    .ThenInclude(a => a.Images)
                .Include(ut => ut.User)
                .FirstOrDefault(userTrack => userTrack.User.Id == userId && userTrack.Track.Id == trackId)
                ?? throw Logger.UserTrackNotFound(userId, trackId);

            var res = new UserTrackResponse(){
                TotalTime = userTrack.TotalTime,
                Track = new TrackResponse(userTrack.Track),
                UserId = userTrack.User.Id
            };
            return res;
        }


        public async Task<UserAlbumResponse> PostUserAblum(string userId, UserAlbumRequest request)
        {
            var userAlbum = PrestigeDb.UserAlbums
                .Include(ua => ua.Album)
                    .ThenInclude(a => a.Images)
                .Include(ua => ua.Album.Artists)
                    .ThenInclude(a => a.Images)
                .Include(ua => ua.User)
                .FirstOrDefault(userAlbum => userAlbum.User.Id == userId && userAlbum.Album.Id == request.AlbumId);
            if (userAlbum != null)
            {
                userAlbum.IncrementTotalTime(request.TotalTime);
                PrestigeDb.SaveChanges();
                return new UserAlbumResponse()
                {
                    TotalTime = userAlbum.TotalTime,
                    Album = new AlbumResponse(userAlbum.Album),
                    UserId = userAlbum.User.Id
                };
            }

            var user = PrestigeDb.Users.FirstOrDefault(user => user.Id == userId)
                ?? throw Logger.UserNotFound(userId);
            var album = PrestigeDb.Albums
                .Include(album => album.Artists)
                    .ThenInclude(artist => artist.Images)
                .Include(album => album.Images)
                .FirstOrDefault(album => album.Id == request.AlbumId);
            
            if (album == null)
            {
                // Album doesn't exist, fetch from Spotify and create it
                var spotifyAlbum = await _spotifyServices.GetAlbumByIdAsync(request.AlbumId);
                if (spotifyAlbum == null)
                {
                    throw Logger.AlbumNotFound(request.AlbumId);
                }
                
                // Reload the album from database after it was created by SpotifyServices
                album = PrestigeDb.Albums
                    .Include(album => album.Artists)
                        .ThenInclude(artist => artist.Images)
                    .Include(album => album.Images)
                    .FirstOrDefault(album => album.Id == request.AlbumId)
                    ?? throw Logger.AlbumNotFound(request.AlbumId);
            }
            userAlbum = new UserAlbum(user, request.TotalTime, album);

            PrestigeDb.UserAlbums.Add(userAlbum);
            PrestigeDb.SaveChanges();
            return new UserAlbumResponse()
            {
                TotalTime = userAlbum.TotalTime,
                Album = new AlbumResponse(userAlbum.Album),
                UserId = userAlbum.User.Id
            };
        }

        public UserAlbumResponse GetUserAlbum(string userId, string albumId)
        {
            var userAlbum = PrestigeDb.UserAlbums
                .Include(ua => ua.Album)
                    .ThenInclude(a => a.Images)
                .Include(ua => ua.Album.Artists)
                    .ThenInclude(a => a.Images)
                .Include(ua => ua.User)
                .FirstOrDefault(userAlbum => userAlbum.User.Id == userId && userAlbum.Album.Id == albumId)
                ?? throw Logger.UserAlbumNotFound(userId, albumId);
            return new UserAlbumResponse()
            {
                TotalTime = userAlbum.TotalTime,
                Album = new AlbumResponse(userAlbum.Album),
                UserId = userAlbum.User.Id
            };
        }

        public async Task<UserArtistResponse> PostUserArtist(string userId, UserArtistRequest request)
        {
            var userArtist = PrestigeDb.UserArtists
                .Include(ua => ua.Artist)
                    .ThenInclude(a => a.Images)
                .Include(ua => ua.User)
                .FirstOrDefault(userArtist => userArtist.User.Id == userId && userArtist.Artist.Id == request.ArtistId);
            if (userArtist != null)
            {
                userArtist.IncrementTotalTime(request.TotalTime);
                PrestigeDb.SaveChanges();
                return new UserArtistResponse()
                {
                    TotalTime = userArtist.TotalTime,
                    Artist = new ArtistResponse(userArtist.Artist),
                    UserId = userArtist.User.Id
                };
            }

            var user = PrestigeDb.Users.FirstOrDefault(user => user.Id == userId)
                ?? throw Logger.UserNotFound(userId);
            var artist = PrestigeDb.Artists
                .Include(artist => artist.Images)
                .FirstOrDefault(artist => artist.Id == request.ArtistId);
            
            if (artist == null)
            {
                // Artist doesn't exist, fetch from Spotify and create it
                var spotifyArtist = await _spotifyServices.GetArtistByIdAsync(request.ArtistId);
                if (spotifyArtist == null)
                {
                    throw Logger.ArtistNotFound(request.ArtistId);
                }
                
                // Reload the artist from database after it was created by SpotifyServices
                artist = PrestigeDb.Artists
                    .Include(artist => artist.Images)
                    .FirstOrDefault(artist => artist.Id == request.ArtistId)
                    ?? throw Logger.ArtistNotFound(request.ArtistId);
            }
            userArtist = new UserArtist(user, request.TotalTime, artist);

            PrestigeDb.UserArtists.Add(userArtist);
            PrestigeDb.SaveChanges();
            return new UserArtistResponse()
            {
                TotalTime = userArtist.TotalTime,
                Artist = new ArtistResponse(userArtist.Artist),
                UserId = userArtist.User.Id
            };
        }

        public UserArtistResponse GetUserArtist(string userId, string artistId)
        {
            var userArtist = PrestigeDb.UserArtists
                .Include(ua => ua.Artist)
                    .ThenInclude(a => a.Images)
                .Include(ua => ua.User)
                .FirstOrDefault(userArtist => userArtist.User.Id == userId && userArtist.Artist.Id == artistId)
                ?? throw Logger.UserArtistNotFound(userId, artistId);
            return new UserArtistResponse()
            {
                TotalTime = userArtist.TotalTime,
                Artist = new ArtistResponse(userArtist.Artist),
                UserId = userArtist.User.Id
            };
        }

        public async Task TogglePinUserTrack(string userId, string trackId)
        {
            var userTrack = PrestigeDb.UserTracks
                .Include(ut => ut.User)
                .Include(ut => ut.Track)
                .FirstOrDefault(ut => ut.User.Id == userId && ut.Track.Id == trackId)
                ?? throw Logger.TrackNotFound(trackId);

            userTrack.ToggleIsPinned();
            await PrestigeDb.SaveChangesAsync();
        }

        public async Task TogglePinUserAlbum(string userId, string albumId)
        {
            var userAlbum = PrestigeDb.UserAlbums
                .Include(ua => ua.User)
                .Include(ua => ua.Album)
                .FirstOrDefault(ua => ua.User.Id == userId && ua.Album.Id == albumId)
                ?? throw Logger.AlbumNotFound(albumId);

            userAlbum.ToggleIsPinned();
            await PrestigeDb.SaveChangesAsync();
        }

        public async Task TogglePinUserArtist(string userId, string artistId)
        {
            var userArtist = PrestigeDb.UserArtists
                .Include(ua => ua.User)
                .Include(ua => ua.Artist)
                .FirstOrDefault(ua => ua.User.Id == userId && ua.Artist.Id == artistId)
                ?? throw Logger.ArtistNotFound(artistId);

            userArtist.ToggleIsPinned();
            await PrestigeDb.SaveChangesAsync();
        }

        public object GetPinnedItems(string userId)
        {
            var pinnedTracks = PrestigeDb.UserTracks
                .Include(ut => ut.Track)
                    .ThenInclude(t => t.Album)
                        .ThenInclude(a => a.Images)
                .Include(ut => ut.Track.Album.Artists)
                .Include(ut => ut.Track.Artists)
                    .ThenInclude(a => a.Images)
                .Where(ut => ut.User.Id == userId && ut.IsPinned)
                .Select(ut => new UserTrackResponse
                {
                    TotalTime = ut.TotalTime,
                    Track = new TrackResponse(ut.Track),
                    UserId = ut.User.Id,
                    IsFavorite = ut.IsFavorite,
                    IsPinned = ut.IsPinned
                })
                .ToList();

            var pinnedAlbums = PrestigeDb.UserAlbums
                .Include(ua => ua.Album)
                    .ThenInclude(a => a.Images)
                .Include(ua => ua.Album.Artists)
                    .ThenInclude(a => a.Images)
                .Where(ua => ua.User.Id == userId && ua.IsPinned)
                .Select(ua => new UserAlbumResponse
                {
                    TotalTime = ua.TotalTime,
                    Album = new AlbumResponse(ua.Album),
                    UserId = ua.User.Id,
                    IsFavorite = ua.IsFavorite,
                    IsPinned = ua.IsPinned
                })
                .ToList();

            var pinnedArtists = PrestigeDb.UserArtists
                .Include(ua => ua.Artist)
                    .ThenInclude(a => a.Images)
                .Where(ua => ua.User.Id == userId && ua.IsPinned)
                .Select(ua => new UserArtistResponse
                {
                    TotalTime = ua.TotalTime,
                    Artist = new ArtistResponse(ua.Artist),
                    UserId = ua.User.Id,
                    IsFavorite = ua.IsFavorite,
                    IsPinned = ua.IsPinned
                })
                .ToList();

            return new
            {
                tracks = pinnedTracks,
                albums = pinnedAlbums,
                artists = pinnedArtists
            };
        }

        public async Task<object> GetAlbumTracksWithRankings(string userId, string albumId)
        {
            // Get all tracks in the album from our database
            var albumTracks = await PrestigeDb.Tracks
                .Include(t => t.Album)
                .Include(t => t.Artists)
                    .ThenInclude(a => a.Images)
                .Where(t => t.Album.Id == albumId)
                .OrderBy(t => t.Name) // Default order by name, can be enhanced with track_number later
                .ToListAsync();

            // Get user ratings for tracks in this album
            var userTracks = await PrestigeDb.UserTracks
                .Include(ut => ut.Track)
                .Where(ut => ut.User.Id == userId && ut.Track.Album.Id == albumId)
                .ToListAsync();

            // Create lookup for user data
            var userTrackLookup = userTracks.ToDictionary(ut => ut.Track.Id, ut => ut);

            // Create response with ranking within album context
            var tracksWithRankings = albumTracks.Select(track =>
            {
                var userTrack = userTrackLookup.ContainsKey(track.Id) ? userTrackLookup[track.Id] : null;
                return new
                {
                    TrackId = track.Id,
                    TrackName = track.Name,
                    Artists = track.Artists.Select(a => new { Id = a.Id, Name = a.Name }),
                    DurationMs = track.DurationMs,
                    UserListeningTime = userTrack?.TotalTime ?? 0,
                    UserRating = userTrack?.PersonalRatingScore,
                    HasUserRating = userTrack != null,
                    IsPinned = userTrack?.IsPinned ?? false,
                    IsFavorite = userTrack?.IsFavorite ?? false
                };
            }).ToList();

            // Calculate rankings based on user listening time within this album
            var rankedTracks = tracksWithRankings
                .Where(t => t.HasUserRating)
                .OrderByDescending(t => t.UserListeningTime)
                .Select((track, index) => new
                {
                    track.TrackId,
                    track.TrackName,
                    track.Artists,
                    track.DurationMs,
                    track.UserListeningTime,
                    track.UserRating,
                    track.HasUserRating,
                    track.IsPinned,
                    track.IsFavorite,
                    AlbumRanking = index + 1
                })
                .ToList();

            // Add unrated tracks with null ranking
            var unratedTracks = tracksWithRankings
                .Where(t => !t.HasUserRating)
                .Select(track => new
                {
                    track.TrackId,
                    track.TrackName,
                    track.Artists,
                    track.DurationMs,
                    track.UserListeningTime,
                    track.UserRating,
                    track.HasUserRating,
                    track.IsPinned,
                    track.IsFavorite,
                    AlbumRanking = (int?)null
                })
                .ToList();

            var allTracks = rankedTracks.Cast<object>().Concat(unratedTracks.Cast<object>()).ToList();

            return new
            {
                AlbumId = albumId,
                TotalTracks = albumTracks.Count,
                RatedTracks = rankedTracks.Count,
                AllTracksRated = rankedTracks.Count == albumTracks.Count,
                Tracks = allTracks
            };
        }
    }
}
