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

        public PrestigeServices(PrestigeContext db, ILogger<PrestigeServices> logger, ClaimsPrincipal principal, IConfiguration config) : base(db, logger, principal, config)
        {

        }

        public UserTrackResponse PostUserTrack(string userId, UserTrackRequest request)
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
                PostUserAblum(userId, new UserAlbumRequest()
                {
                    AlbumId = userTrack.Track.Album.Id,
                    TotalTime = request.TotalTime
                }
                );
                PostUserArtist(userId, new UserArtistRequest()
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
                .FirstOrDefault(track => track.Id == request.TrackId)
                ?? throw Logger.TrackNotFound(request.TrackId);
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
            var postAlbumResponse = PostUserAblum(userId, new UserAlbumRequest()
            {
                AlbumId = userTrack.Track.Album.Id,
                TotalTime = request.TotalTime
            }
            ) ?? throw Logger.UserAlbumNotFound(userId, userTrack.Track.Album.Id);
            var postArtistResponse = PostUserArtist(userId, new UserArtistRequest()
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


        public UserAlbumResponse PostUserAblum(string userId, UserAlbumRequest request)
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
                .FirstOrDefault(album => album.Id == request.AlbumId)
                ?? throw Logger.AlbumNotFound(request.AlbumId);
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

        public UserArtistResponse PostUserArtist(string userId, UserArtistRequest request)
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
                .FirstOrDefault(artist => artist.Id == request.ArtistId)
                ?? throw Logger.ArtistNotFound(request.ArtistId);
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
    }
}
