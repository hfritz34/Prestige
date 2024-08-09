using Microsoft.EntityFrameworkCore;
using Prestige.Api.Data;
using Prestige.Api.Domain;
using Prestige.Api.Endpoints.Prestige.RequestResponse;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;
using Prestige.Api.Logging;
using Prestige.Api.Endpoints.Spotify.RequestResponse;

namespace Prestige.Api.Endpoints.Profile
{
    public class ProfileServices : BaseService
    {
        private string UserAuthId => Principal.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? throw new Exception("User not found");

        public ProfileServices(PrestigeContext prestigeDb, ILogger<ProfileServices> logger, ClaimsPrincipal principal, IConfiguration config)
            : base(prestigeDb, logger, principal, config)
        {
        }

        const int MAX_DISPLAYED = 25;


        public async Task<IEnumerable<UserTrackResponse>> GetTopTracksAsync(string userId)
        {
            if (userId != UserAuthId.Split("|").Last())
            {
                throw new Exception("Unauthorized");
            }

            var userTracks = await PrestigeDb.UserTracks
                .Where(ut => ut.User.Id == userId)
                .OrderByDescending(ut => ut.TotalTime)
                .Take(MAX_DISPLAYED)
                .Include(ut => ut.Track)
                    .ThenInclude(t => t.Album)
                        .ThenInclude(a => a.Images)
                .Include(ut => ut.Track)
                    .ThenInclude(t => t.Artists)
                        .ThenInclude(ar => ar.Images)
                .Include(ut => ut.Track.Album.Artists)
                    .ThenInclude(ar => ar.Images)
                .Include(ut => ut.User)
                .ToListAsync();

            var topTracks = userTracks.Select(ut => new UserTrackResponse()
            {
                Track = new TrackResponse(ut.Track),
                TotalTime = ut.TotalTime,
                UserId = ut.User.Id
            }).ToList();

            return topTracks;
        }

        public async Task<IEnumerable<UserAlbumResponse>> GetTopAlbumsAsync(string userId)
        {
            if (userId != UserAuthId.Split("|").Last())
            {
                throw new Exception("Unauthorized");
            }

            var userAlbums = await PrestigeDb.UserAlbums
                .Where(ua => ua.User.Id == userId)
                .OrderByDescending(ua => ua.TotalTime)
                .Take(MAX_DISPLAYED)
                .Include(ua => ua.Album)
                    .ThenInclude(a => a.Images)
                .Include(ua => ua.Album)
                    .ThenInclude(a => a.Artists)
                        .ThenInclude(ar => ar.Images)
                .Include(ua => ua.User)
                .ToListAsync();

            var topAlbums = userAlbums.Select(ua => new UserAlbumResponse()
            {
                UserId = ua.User.Id,
                Album = new AlbumResponse(ua.Album),
                TotalTime = ua.TotalTime
            })
                .ToList();

            return topAlbums;
        }

        public async Task<IEnumerable<UserArtistResponse>> GetTopArtistsAsync(string userId)
        {
            if (userId != UserAuthId.Split("|").Last())
            {
                throw new Exception("Unauthorized");
            }

            var userArtists = await PrestigeDb.UserArtists
                .Where(ua => ua.User.Id == userId)
                .OrderByDescending(ua => ua.TotalTime)
                .Take(MAX_DISPLAYED)
                .Include(ua => ua.Artist)
                    .ThenInclude(a => a.Images)
                .Include(ua => ua.User)
                .ToListAsync();

            var topArtists = userArtists.Select(ua => new UserArtistResponse()
            {
                Artist = new ArtistResponse(ua.Artist),
                UserId = ua.User.Id,
                TotalTime = ua.TotalTime
            }).ToList();

            return topArtists;
        }

        public async Task<List<RecentlyPlayedResponse>> GetRecentlyPlayedAsync(string userId)
        {
            if (userId != UserAuthId.Split("|").Last())
            {
                throw new Exception("Unauthorized");
            }

            var accessToken = await GetAccessTokenAsync(userId);
            var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            var spotifyRecentlyPlayed = "https://api.spotify.com/v1/me/player/recently-played?limit=50";

            var response = await client.GetAsync(spotifyRecentlyPlayed);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();

            var spotifyResponse = JsonSerializer.Deserialize<SpotifyRecentlyPlayedResponse>(content);

            if (spotifyResponse?.Items == null)
            {
                throw new Exception("Invalid response from Spotify API.");
            }

            var recentlyPlayed = spotifyResponse.Items.Select(item => new RecentlyPlayedResponse(
                item.Track?.Name ?? "Unknown Track",
                item.Track?.Artists.FirstOrDefault()?.Name ?? "Unknown Artist",
                item.Track?.Album.Images.FirstOrDefault()?.Url ?? "No Image Available",
                Guid.NewGuid().ToString()

            )).ToList();

            return recentlyPlayed;
        }
        public IEnumerable<UserTrackResponse> GetFavoriteTracks(string id)
        {
            return PrestigeDb.UserTracks
                .Include(ut => ut.Track)
                    .ThenInclude(t => t.Album)
                        .ThenInclude(a => a.Images)
                .Include(ut => ut.Track.Album.Artists)
                    .ThenInclude(ar => ar.Images)
                .Include(ut => ut.Track.Artists)
                    .ThenInclude(ar => ar.Images)
                .Where(ut => ut.User.Id == id && ut.IsFavorite)
                .Select(ut => new UserTrackResponse()
                {
                    UserId = ut.User.Id,
                    Track = new TrackResponse(ut.Track),
                    TotalTime = ut.TotalTime
                })
                .ToList();
        }

        public IEnumerable<UserTrackResponse> PatchFavoriteTrack(string id, string trackId)
        {
            var thisTrack = PrestigeDb.UserTracks
                .Include(ut => ut.Track)
                    .ThenInclude(t => t.Album)
                        .ThenInclude(a => a.Images)
                .Include(ut => ut.Track.Album.Artists)
                    .ThenInclude(ar => ar.Images)
                .Include(ut => ut.Track.Artists)
                    .ThenInclude(ar => ar.Images)
                .Include(ut => ut.User)
                .FirstOrDefault(userTrack => userTrack.User.Id == id && userTrack.Track.Id == trackId);

            if (thisTrack == null)
            {
                PrestigeDb.UserTracks.Add(
                    new UserTrack(
                        PrestigeDb.Users.FirstOrDefault(u => u.Id == id) ?? throw Logger.UserNotFound(id),
                        0,
                        PrestigeDb.Tracks.FirstOrDefault(t => t.Id == trackId) ?? throw Logger.TrackNotFound(trackId)
                    )
                );
                PrestigeDb.SaveChanges();
                thisTrack = PrestigeDb.UserTracks
                    .Include(ut => ut.Track)
                        .ThenInclude(t => t.Album)
                            .ThenInclude(a => a.Images)
                    .Include(ut => ut.Track.Album.Artists)
                        .ThenInclude(ar => ar.Images)
                    .Include(ut => ut.Track.Artists)
                        .ThenInclude(ar => ar.Images)
                    .Include(ut => ut.User)
                    .FirstOrDefault(userTrack => userTrack.User.Id == id && userTrack.Track.Id == trackId)
                    ?? throw Logger.UserTrackNotFound(id, trackId);
            }

            var totalFavoriteTracks = PrestigeDb.UserTracks
                .Where(ut => ut.User.Id == id && ut.IsFavorite)
                .Count();

            if (totalFavoriteTracks >= 10 && !thisTrack.IsFavorite)
            {
                throw Logger.FavoritesLengthExceeded(id, "Track");
            }

            thisTrack.ToggleIsFavorite();
            PrestigeDb.SaveChanges();

            return PrestigeDb.UserTracks
                .Include(ut => ut.Track)
                    .ThenInclude(t => t.Album)
                        .ThenInclude(a => a.Images)
                .Include(ut => ut.Track.Album.Artists)
                    .ThenInclude(ar => ar.Images)
                .Include(ut => ut.Track.Artists)
                    .ThenInclude(ar => ar.Images)
                .Include(ut => ut.User)
                .Where(ut => ut.User.Id == id && ut.IsFavorite)
                .Select(ut => new UserTrackResponse()
                {
                    UserId = ut.User.Id,
                    Track = new TrackResponse(ut.Track),
                    TotalTime = ut.TotalTime
                })
                .ToList();
        }

        public IEnumerable<UserAlbumResponse> GetFavoriteAlbums(string id)
        {
            return PrestigeDb.UserAlbums
                .Include(ua => ua.Album)
                    .ThenInclude(a => a.Images)
                .Include(ua => ua.Album.Artists)
                    .ThenInclude(ar => ar.Images)
                .Where(ua => ua.User.Id == id && ua.IsFavorite)
                .Select(ua => new UserAlbumResponse()
                {
                    UserId = ua.User.Id,
                    Album = new AlbumResponse(ua.Album),
                    TotalTime = ua.TotalTime
                })
                .ToList();
        }

        public IEnumerable<UserAlbumResponse> PatchFavoriteAlbum(string id, string albumId)
        {
            var thisAlbum = PrestigeDb.UserAlbums
                .Include(ua => ua.Album)
                .Include(ua => ua.User)
                .FirstOrDefault(userAlbum => userAlbum.User.Id == id && userAlbum.Album.Id == albumId);

            if (thisAlbum == null)
            {
                PrestigeDb.UserAlbums.Add(
                    new UserAlbum(
                        PrestigeDb.Users.FirstOrDefault(u => u.Id == id) ?? throw Logger.UserNotFound(id),
                        0,
                        PrestigeDb.Albums.FirstOrDefault(a => a.Id == albumId) ?? throw Logger.AlbumNotFound(albumId)
                    )
                );
                PrestigeDb.SaveChanges();
                thisAlbum = PrestigeDb.UserAlbums
                    .Include(ua => ua.Album)
                        .ThenInclude(a => a.Images)
                    .Include(ua => ua.Album.Artists)
                        .ThenInclude(ar => ar.Images)
                    .Include(ua => ua.User)
                    .FirstOrDefault(userAlbum => userAlbum.User.Id == id && userAlbum.Album.Id == albumId)
                    ?? throw Logger.UserAlbumNotFound(id, albumId);
            }

            var totalFavoriteAlbums = PrestigeDb.UserAlbums
                .Where(ua => ua.User.Id == id && ua.IsFavorite)
                .Count();

            if (totalFavoriteAlbums >= 10 && !thisAlbum.IsFavorite)
            {
                throw Logger.FavoritesLengthExceeded(id, "Album");
            }

            thisAlbum.ToggleIsFavorite();
            PrestigeDb.SaveChanges();

            return PrestigeDb.UserAlbums
                .Include(ua => ua.Album)
                        .ThenInclude(a => a.Images)
                .Include(ua => ua.Album)
                    .ThenInclude(a => a.Artists)
                        .ThenInclude(ar => ar.Images)
                .Include(ua => ua.User)
                .Where(ua => ua.User.Id == id && ua.IsFavorite)
                .Select(ua => new UserAlbumResponse()
                {
                    UserId = ua.User.Id,
                    Album = new AlbumResponse(ua.Album),
                    TotalTime = ua.TotalTime
                })
                .ToList();
        }

        public IEnumerable<UserArtistResponse> GetFavoriteArtists(string id)
        {
            return PrestigeDb.UserArtists
                .Include(ua => ua.Artist)
                    .ThenInclude(a => a.Images)
                .Where(ua => ua.User.Id == id && ua.IsFavorite)
                .Select(ua => new UserArtistResponse()
                {
                    UserId = ua.User.Id,
                    Artist = new ArtistResponse(ua.Artist),
                    TotalTime = ua.TotalTime
                })
                .ToList();
        }

        public IEnumerable<UserArtistResponse> PatchFavoriteArtist(string id, string artistId)
        {
            var thisArtist = PrestigeDb.UserArtists
                .Include(ua => ua.Artist)
                    .ThenInclude(a => a.Images)
                .Include(ua => ua.User)
                .FirstOrDefault(userArtist => userArtist.User.Id == id && userArtist.Artist.Id == artistId);

            if (thisArtist == null)
            {
                PrestigeDb.UserArtists.Add(
                    new UserArtist(
                        PrestigeDb.Users.FirstOrDefault(u => u.Id == id) ?? throw Logger.UserNotFound(id),
                        0,
                        PrestigeDb.Artists.FirstOrDefault(a => a.Id == artistId) ?? throw Logger.ArtistNotFound(artistId)
                    )
                );
                PrestigeDb.SaveChanges();
                thisArtist = PrestigeDb.UserArtists
                    .Include(ua => ua.Artist)
                        .ThenInclude(a => a.Images)
                    .Include(ua => ua.User)
                    .FirstOrDefault(userArtist => userArtist.User.Id == id && userArtist.Artist.Id == artistId)
                    ?? throw Logger.UserArtistNotFound(id, artistId);
            }

            var totalFavoriteArtists = PrestigeDb.UserArtists
                .Where(ua => ua.User.Id == id && ua.IsFavorite)
                .Count();

            if (totalFavoriteArtists >= 10 && !thisArtist.IsFavorite)
            {
                throw Logger.FavoritesLengthExceeded(id, "Artist");
            }

            thisArtist.ToggleIsFavorite();
            PrestigeDb.SaveChanges();

            return PrestigeDb.UserArtists
                .Include(ua => ua.Artist)
                    .ThenInclude(a => a.Images)
                .Include(ua => ua.User)
                .Where(ua => ua.User.Id == id && ua.IsFavorite)
                .Select(ua => new UserArtistResponse()
                {
                    UserId = ua.User.Id,
                    Artist = new ArtistResponse(ua.Artist),
                    TotalTime = ua.TotalTime
                })
                .ToList();
        }

    }
}