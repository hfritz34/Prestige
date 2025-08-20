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

            // If we have very few tracks in our database, fetch from Spotify to get complete listing
            var allTrackData = new List<object>();
            
            // Add database tracks
            foreach (var track in albumTracks)
            {
                allTrackData.Add(new
                {
                    Id = track.Id,
                    Name = track.Name,
                    Artists = track.Artists,
                    DurationMs = track.DurationMs,
                    TrackNumber = 0, // Database tracks don't have track numbers yet, will use 0 as fallback
                    IsFromDatabase = true
                });
            }
            
            // Always fetch from Spotify to get complete track listing, regardless of database track count
            {
                try
                {
                    var spotifyTracks = await _spotifyServices.GetAlbumTracksAsync(albumId);
                    foreach (var spotifyTrack in spotifyTracks)
                    {
                        // Check if track already exists in our database
                        var existingTrack = albumTracks.FirstOrDefault(t => t.Id == spotifyTrack.Id);
                        if (existingTrack == null)
                        {
                            // Add the Spotify track to our temporary list
                            allTrackData.Add(new
                            {
                                Id = spotifyTrack.Id,
                                Name = spotifyTrack.Name,
                                Artists = spotifyTrack.Artists?.Select(a => new { Id = a.Id, Name = a.Name }) ?? Enumerable.Empty<object>(),
                                DurationMs = spotifyTrack.DurationMs,
                                TrackNumber = spotifyTrack.TrackNumber,
                                IsFromDatabase = false
                            });
                        }
                        else
                        {
                            // Update the existing database track with correct track number
                            var dbTrackIndex = allTrackData.FindIndex(t => ((dynamic)t).Id == existingTrack.Id);
                            if (dbTrackIndex >= 0)
                            {
                                allTrackData[dbTrackIndex] = new
                                {
                                    Id = existingTrack.Id,
                                    Name = existingTrack.Name,
                                    Artists = existingTrack.Artists,
                                    DurationMs = existingTrack.DurationMs,
                                    TrackNumber = spotifyTrack.TrackNumber, // Use Spotify track number
                                    IsFromDatabase = true
                                };
                            }
                        }
                    }
                }
                catch (System.Exception ex)
                {
                    // Log error but continue with database tracks only
                    Logger.LogWarning($"Failed to fetch tracks from Spotify for album {albumId}: {ex.Message}");
                }
            }

            // Get user ratings for tracks in this album
            var userTracks = await PrestigeDb.UserTracks
                .Include(ut => ut.Track)
                .Where(ut => ut.User.Id == userId && ut.Track.Album.Id == albumId)
                .ToListAsync();

            // Create lookup for user data
            var userTrackLookup = userTracks.ToDictionary(ut => ut.Track.Id, ut => ut);

            // Sort by track number to maintain album order
            var sortedTrackData = allTrackData.OrderBy(trackData =>
            {
                dynamic track = trackData;
                return (int)track.TrackNumber;
            }).ToList();

            // Create response with ranking within album context
            var tracksWithRankings = sortedTrackData.Select(trackData =>
            {
                dynamic track = trackData;
                var userTrack = userTrackLookup.ContainsKey((string)track.Id) ? userTrackLookup[(string)track.Id] : null;
                return new
                {
                    TrackId = (string)track.Id,
                    TrackName = (string)track.Name,
                    Artists = track.Artists,
                    DurationMs = (int)track.DurationMs,
                    TrackNumber = (int)track.TrackNumber,
                    UserListeningTime = userTrack?.TotalTime ?? 0,
                    UserRating = userTrack?.PersonalRatingScore,
                    HasUserRating = userTrack?.RatingPosition != null,
                    IsPinned = userTrack?.IsPinned ?? false,
                    IsFavorite = userTrack?.IsFavorite ?? false,
                    IsFromDatabase = (bool)track.IsFromDatabase
                };
            }).ToList();

            // Create final tracks list with proper album ranking for rated tracks, ordered by track number
            var finalTracks = tracksWithRankings
                .Select(track =>
                {
                    var userTrackData = userTrackLookup.ContainsKey(track.TrackId) ? userTrackLookup[track.TrackId] : null;
                    return new
                    {
                        track.TrackId,
                        track.TrackName,
                        track.Artists,
                        track.DurationMs,
                        track.TrackNumber,
                        track.UserListeningTime,
                        track.UserRating,
                        track.HasUserRating,
                        track.IsPinned,
                        track.IsFavorite,
                        track.IsFromDatabase,
                        AlbumRanking = track.HasUserRating ? userTrackData?.RatingPosition : (int?)null
                    };
                })
                .OrderBy(t => t.TrackNumber) // Order by actual album track number
                .Cast<object>()
                .ToList();

            var ratedTracksCount = tracksWithRankings.Count(t => t.HasUserRating);

            return new
            {
                AlbumId = albumId,
                TotalTracks = allTrackData.Count,
                RatedTracks = ratedTracksCount,
                AllTracksRated = ratedTracksCount == allTrackData.Count,
                Tracks = finalTracks
            };
        }

        public async Task<object> GetArtistAlbumsWithUserActivity(string userId, string artistId)
        {
            // Get all albums by this artist that the user has rated tracks for
            var userAlbumsWithActivity = await PrestigeDb.UserTracks
                .Include(ut => ut.Track)
                    .ThenInclude(t => t.Album)
                        .ThenInclude(a => a.Images)
                .Include(ut => ut.Track.Album.Artists)
                .Where(ut => ut.User.Id == userId && 
                           ut.Track.Artists.Any(a => a.Id == artistId) &&
                           ut.RatingPosition != null) // Only albums where user has rated tracks
                .GroupBy(ut => ut.Track.Album.Id)
                .Select(g => new
                {
                    Album = g.First().Track.Album,
                    RatedTracksCount = g.Count(),
                    TotalListeningTime = g.Sum(ut => ut.TotalTime),
                    HighestRankedTrack = g.OrderBy(ut => ut.RatingPosition).First()
                })
                .ToListAsync();

            // Transform to response format
            var albumsWithActivity = userAlbumsWithActivity.Select(album => new
            {
                AlbumId = album.Album.Id,
                AlbumName = album.Album.Name,
                AlbumImage = album.Album.Images?.FirstOrDefault()?.Url,
                ArtistName = album.Album.Artists?.FirstOrDefault()?.Name,
                RatedTracksCount = album.RatedTracksCount,
                TotalListeningTime = album.TotalListeningTime,
                BestTrackName = album.HighestRankedTrack.Track.Name,
                BestTrackRanking = album.HighestRankedTrack.RatingPosition
            }).OrderByDescending(a => a.RatedTracksCount)
              .ThenByDescending(a => a.TotalListeningTime)
              .ToList();

            return new
            {
                ArtistId = artistId,
                AlbumsWithUserActivity = albumsWithActivity,
                TotalAlbumsWithActivity = albumsWithActivity.Count
            };
        }
    }
}
