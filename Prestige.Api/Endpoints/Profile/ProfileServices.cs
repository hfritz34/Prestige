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
using Prestige.Api.Configuration;
using Exception = Prestige.Api.Exceptions.Exception;

namespace Prestige.Api.Endpoints.Profile
{
    public class ProfileServices : BaseService
    {
        private string? UserAuthId => Principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        public ProfileServices(PrestigeContext prestigeDb, ILogger<ProfileServices> logger, ClaimsPrincipal principal, IConfiguration config)
            : base(prestigeDb, logger, principal, config)
        {
        }

        const int MAX_DISPLAYED = 99;


        public async Task<IEnumerable<UserTrackResponse>> GetTopTracksAsync(string userId)
        {
            if (string.IsNullOrEmpty(UserAuthId))
            {
                throw new Exception(10001, "User authentication required");
            }
            
            var currentUserId = UserAuthId.Split("|").Last();
            if (userId != currentUserId)
            {
                _logger.LogWarning($"Unauthorized access attempt. Requested user ID: {userId}, Current user ID: {currentUserId}");
                throw Logger.UserUnauthorized(userId);
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
                UserId = ut.User.Id,
                IsFavorite = ut.IsFavorite,
                IsPinned = ut.IsPinned,
                PrestigeTier = PrestigeThresholds.CalculatePrestigeTier(ut.TotalTime, "track")
            }).ToList();

            return topTracks;
        }

        public async Task<IEnumerable<UserAlbumResponse>> GetTopAlbumsAsync(string userId)
        {
            if (string.IsNullOrEmpty(UserAuthId) || userId != UserAuthId.Split("|").Last())
            {
                throw Logger.UserUnauthorized(userId);
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
                TotalTime = ua.TotalTime,
                IsFavorite = ua.IsFavorite,
                IsPinned = ua.IsPinned,
                PrestigeTier = PrestigeThresholds.CalculatePrestigeTier(ua.TotalTime, "album")
            })
                .ToList();

            return topAlbums;
        }

        public async Task<IEnumerable<UserArtistResponse>> GetTopArtistsAsync(string userId)
        {
            if (string.IsNullOrEmpty(UserAuthId) || userId != UserAuthId.Split("|").Last())
            {
                throw Logger.UserUnauthorized(userId);
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
                TotalTime = ua.TotalTime,
                IsFavorite = ua.IsFavorite,
                IsPinned = ua.IsPinned,
                PrestigeTier = PrestigeThresholds.CalculatePrestigeTier(ua.TotalTime, "artist")
            }).ToList();

            return topArtists;
        }

        public async Task<List<RecentlyPlayedResponse>> GetRecentlyPlayedAsync(string userId)
        {
            if (string.IsNullOrEmpty(UserAuthId))
            {
                throw new Exception(10001, "User authentication required");
            }
            
            var currentUserId = UserAuthId.Split("|").Last();
            _logger.LogInformation($"BEGIN GetRecentlyPlayedAsync: Current user ID: {currentUserId}, Requested user ID: {userId}");

            if (userId != currentUserId)
            {
                _logger.LogWarning($"GetRecentlyPlayedAsync: Unauthorized access attempt. Requested user ID: {userId}, Current user ID: {currentUserId}");
                throw Logger.UserUnauthorized(userId);
            }

            try
            {
                _logger.LogInformation($"GetRecentlyPlayedAsync: Getting access token for user {userId}");
                var accessToken = await GetAccessTokenAsync(userId);
                _logger.LogInformation($"GetRecentlyPlayedAsync: Successfully got access token for user {userId}. Token Length: {accessToken?.Length ?? 0}");

                using var client = new HttpClient();
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                var spotifyRecentlyPlayedUrl = "https://api.spotify.com/v1/me/player/recently-played?limit=50";

                _logger.LogInformation($"GetRecentlyPlayedAsync: Fetching recently played tracks for user {userId} from URL: {spotifyRecentlyPlayedUrl}");
                var response = await client.GetAsync(spotifyRecentlyPlayedUrl);
                _logger.LogInformation($"GetRecentlyPlayedAsync: Initial Spotify API response status: {response.StatusCode}");

                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    _logger.LogWarning("GetRecentlyPlayedAsync: Spotify Access token expired, attempting refresh...");
                    var user = await PrestigeDb.Users.FirstOrDefaultAsync(u => u.Id == userId);
                    if (user == null)
                    {
                         _logger.LogError($"GetRecentlyPlayedAsync: User {userId} not found in database during token refresh attempt.");
                         throw Logger.UserNotFound(userId);
                    }

                    if (string.IsNullOrEmpty(user.RefreshToken))
                    {
                         _logger.LogError($"GetRecentlyPlayedAsync: User {userId} found but has no RefreshToken in database.");
                         throw new Exception(10010, $"User {userId} has no refresh token available for Spotify refresh.");
                    }

                    _logger.LogInformation($"GetRecentlyPlayedAsync: Found user {userId} in database, attempting RefreshSpotifyTokensAsync with RefreshToken Length: {user.RefreshToken?.Length ?? 0}");

                    try
                    {
                        (string newAccessToken, string newRefreshToken) = await RefreshSpotifyTokensAsync(user.RefreshToken);
                        _logger.LogInformation($"GetRecentlyPlayedAsync: Successfully refreshed Spotify tokens. New AccessToken Length: {newAccessToken?.Length ?? 0}, New RefreshToken Length: {newRefreshToken?.Length ?? 0}");

                        user.UpdateTokens(newAccessToken, newRefreshToken, DateTime.UtcNow.AddMinutes(55)); // Use UtcNow
                        await PrestigeDb.SaveChangesAsync();
                        _logger.LogInformation($"GetRecentlyPlayedAsync: Updated user tokens in database for {userId}");

                        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", newAccessToken);
                        _logger.LogInformation($"GetRecentlyPlayedAsync: Retrying Spotify API call with new token to {spotifyRecentlyPlayedUrl}");
                        response = await client.GetAsync(spotifyRecentlyPlayedUrl);
                        _logger.LogInformation($"GetRecentlyPlayedAsync: Retried Spotify API call status: {response.StatusCode}");
                    }
                    catch (Exception tokenEx)
                    {
                        _logger.LogError(tokenEx, $"GetRecentlyPlayedAsync: Error during RefreshSpotifyTokensAsync for user {userId}");
                        throw; // Re-throw the specific token refresh exception
                    }
                }

                var content = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError($"GetRecentlyPlayedAsync: Spotify API error after potential refresh: Status {response.StatusCode}, Content: {content}");
                    throw new Exception(10011, $"Failed to fetch recently played tracks: {response.StatusCode} - {content}");
                }

                _logger.LogInformation($"GetRecentlyPlayedAsync: Successfully retrieved content for user {userId}. Content Length: {content?.Length ?? 0}");

                SpotifyRecentlyPlayedResponse? spotifyResponse = null;
                try
                {
                     spotifyResponse = JsonSerializer.Deserialize<SpotifyRecentlyPlayedResponse>(content);
                     _logger.LogInformation($"GetRecentlyPlayedAsync: Successfully deserialized Spotify response for user {userId}. Items found: {spotifyResponse?.Items?.Count ?? 0}");
                }
                catch (JsonException jsonEx)
                {
                    _logger.LogError(jsonEx, $"GetRecentlyPlayedAsync: JSON deserialization failed for user {userId}. Content: {content}");
                    throw new Exception(10012, "Failed to deserialize Spotify response.", jsonEx);
                }


                if (spotifyResponse?.Items == null)
                {
                    _logger.LogWarning($"GetRecentlyPlayedAsync: Deserialized response from Spotify API has null Items for user {userId}. Content: {content}");
                    throw new Exception(10013, "Invalid response from Spotify API: Items is null after deserialization");
                }

                var tracks = spotifyResponse.Items
                    .Where(item => item.Track != null)
                    .Select(item => new RecentlyPlayedResponse(
                        item.Track.Name ?? "Unknown Track",
                        item.Track.Artists?.FirstOrDefault()?.Name ?? "Unknown Artist",
                        item.Track.Album?.Images?.FirstOrDefault()?.Url ?? "No Image Available",
                        item.Track.Id ?? Guid.NewGuid().ToString()
                    )).ToList();

                _logger.LogInformation($"GetRecentlyPlayedAsync: Successfully processed {tracks.Count} recently played tracks for user {userId}");
                _logger.LogInformation($"END GetRecentlyPlayedAsync: For user {userId}");
                return tracks;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"GetRecentlyPlayedAsync: Unhandled exception for user {userId} before returning to controller.");
                throw;
            }
        }

        public async Task<List<RecentlyPlayedAlbumResponse>> GetRecentlyPlayedAlbumsAsync(string userId)
        {
            if (string.IsNullOrEmpty(UserAuthId))
            {
                throw new Exception(10001, "User authentication required");
            }
            
            var currentUserId = UserAuthId.Split("|").Last();
            _logger.LogInformation($"BEGIN GetRecentlyPlayedAlbumsAsync: Current user ID: {currentUserId}, Requested user ID: {userId}");

            if (userId != currentUserId)
            {
                _logger.LogWarning($"GetRecentlyPlayedAlbumsAsync: Unauthorized access attempt. Requested user ID: {userId}, Current user ID: {currentUserId}");
                throw Logger.UserUnauthorized(userId);
            }

            try
            {
                _logger.LogInformation($"GetRecentlyPlayedAlbumsAsync: Getting access token for user {userId}");
                var accessToken = await GetAccessTokenAsync(userId);

                using var client = new HttpClient();
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                var spotifyRecentlyPlayedUrl = "https://api.spotify.com/v1/me/player/recently-played?limit=50";

                var response = await client.GetAsync(spotifyRecentlyPlayedUrl);

                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    var user = await PrestigeDb.Users.FirstOrDefaultAsync(u => u.Id == userId);
                    if (user?.RefreshToken != null)
                    {
                        (string newAccessToken, string newRefreshToken) = await RefreshSpotifyTokensAsync(user.RefreshToken);
                        user.UpdateTokens(newAccessToken, newRefreshToken, DateTime.UtcNow.AddMinutes(55));
                        await PrestigeDb.SaveChangesAsync();
                        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", newAccessToken);
                        response = await client.GetAsync(spotifyRecentlyPlayedUrl);
                    }
                }

                var content = await response.Content.ReadAsStringAsync();
                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception(10011, $"Failed to fetch recently played tracks: {response.StatusCode} - {content}");
                }

                var spotifyResponse = JsonSerializer.Deserialize<SpotifyRecentlyPlayedResponse>(content);
                if (spotifyResponse?.Items == null)
                {
                    throw new Exception(10013, "Invalid response from Spotify API: Items is null after deserialization");
                }

                var albums = spotifyResponse.Items
                    .Where(item => item.Track?.Album != null)
                    .GroupBy(item => item.Track.Album.Id)
                    .Select(group => group.First().Track.Album)
                    .Select(album => new RecentlyPlayedAlbumResponse(
                        album.Name ?? "Unknown Album",
                        album.Artists?.FirstOrDefault()?.Name ?? "Unknown Artist",
                        album.Images?.FirstOrDefault()?.Url ?? "No Image Available",
                        album.Id ?? Guid.NewGuid().ToString()
                    )).ToList();

                _logger.LogInformation($"GetRecentlyPlayedAlbumsAsync: Successfully processed {albums.Count} recently played albums for user {userId}");
                return albums;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"GetRecentlyPlayedAlbumsAsync: Unhandled exception for user {userId}");
                throw;
            }
        }

        public async Task<List<RecentlyPlayedArtistResponse>> GetRecentlyPlayedArtistsAsync(string userId)
        {
            if (string.IsNullOrEmpty(UserAuthId))
            {
                throw new Exception(10001, "User authentication required");
            }
            
            var currentUserId = UserAuthId.Split("|").Last();
            _logger.LogInformation($"BEGIN GetRecentlyPlayedArtistsAsync: Current user ID: {currentUserId}, Requested user ID: {userId}");

            if (userId != currentUserId)
            {
                _logger.LogWarning($"GetRecentlyPlayedArtistsAsync: Unauthorized access attempt. Requested user ID: {userId}, Current user ID: {currentUserId}");
                throw Logger.UserUnauthorized(userId);
            }

            try
            {
                _logger.LogInformation($"GetRecentlyPlayedArtistsAsync: Getting access token for user {userId}");
                var accessToken = await GetAccessTokenAsync(userId);

                using var client = new HttpClient();
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                var spotifyRecentlyPlayedUrl = "https://api.spotify.com/v1/me/player/recently-played?limit=50";

                var response = await client.GetAsync(spotifyRecentlyPlayedUrl);

                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    var user = await PrestigeDb.Users.FirstOrDefaultAsync(u => u.Id == userId);
                    if (user?.RefreshToken != null)
                    {
                        (string newAccessToken, string newRefreshToken) = await RefreshSpotifyTokensAsync(user.RefreshToken);
                        user.UpdateTokens(newAccessToken, newRefreshToken, DateTime.UtcNow.AddMinutes(55));
                        await PrestigeDb.SaveChangesAsync();
                        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", newAccessToken);
                        response = await client.GetAsync(spotifyRecentlyPlayedUrl);
                    }
                }

                var content = await response.Content.ReadAsStringAsync();
                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception(10011, $"Failed to fetch recently played tracks: {response.StatusCode} - {content}");
                }

                var spotifyResponse = JsonSerializer.Deserialize<SpotifyRecentlyPlayedResponse>(content);
                if (spotifyResponse?.Items == null)
                {
                    throw new Exception(10013, "Invalid response from Spotify API: Items is null after deserialization");
                }

                // Build distinct list of artist IDs from recently played
                var artistIds = spotifyResponse.Items
                    .Where(item => item.Track?.Artists != null)
                    .SelectMany(item => item.Track!.Artists!)
                    .Select(a => a.Id)
                    .Where(id => !string.IsNullOrWhiteSpace(id))
                    .Distinct()
                    .ToList();

                // Attempt to fetch cached images for these artists from our DB
                var dbArtists = await PrestigeDb.Artists
                    .Where(a => artistIds.Contains(a.Id))
                    .Include(a => a.Images)
                    .ToListAsync();

                var artistImageFromDb = dbArtists
                    .ToDictionary(a => a.Id, a => a.Images.OrderByDescending(img => img.Height).FirstOrDefault()?.Url);

                // Group artists from Spotify response and use cached Spotify artist images from our DB
                var artists = spotifyResponse.Items
                    .Where(item => item.Track?.Artists != null)
                    .SelectMany(item => item.Track!.Artists!)
                    .GroupBy(artist => artist.Id)
                    .Select(group =>
                    {
                        var first = group.First();
                        var id = first.Id ?? Guid.NewGuid().ToString();
                        var name = first.Name ?? "Unknown Artist";

                        // Use artist image stored in SQL DB (cached from Spotify). No album image fallback.
                        var imageUrl = (artistImageFromDb.TryGetValue(id, out var dbUrl) && !string.IsNullOrWhiteSpace(dbUrl))
                            ? dbUrl!
                            : "No Image Available";

                        return new RecentlyPlayedArtistResponse(name, imageUrl, id);
                    })
                    .ToList();

                _logger.LogInformation($"GetRecentlyPlayedArtistsAsync: Successfully processed {artists.Count} recently played artists for user {userId}");
                return artists;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"GetRecentlyPlayedArtistsAsync: Unhandled exception for user {userId}");
                throw;
            }
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
                    TotalTime = ut.TotalTime,
                    PrestigeTier = PrestigeThresholds.CalculatePrestigeTier(ut.TotalTime, "track")
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

            if (totalFavoriteTracks >= 30 && !thisTrack.IsFavorite)
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
                    TotalTime = ut.TotalTime,
                    PrestigeTier = PrestigeThresholds.CalculatePrestigeTier(ut.TotalTime, "track")
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
                    TotalTime = ua.TotalTime,
                    IsFavorite = ua.IsFavorite,
                    IsPinned = ua.IsPinned
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

            if (totalFavoriteAlbums >= 30 && !thisAlbum.IsFavorite)
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
                    TotalTime = ua.TotalTime,
                    IsFavorite = ua.IsFavorite,
                    IsPinned = ua.IsPinned
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
                    TotalTime = ua.TotalTime,
                    IsFavorite = ua.IsFavorite,
                    IsPinned = ua.IsPinned
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

            if (totalFavoriteArtists >= 30 && !thisArtist.IsFavorite)
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
                    TotalTime = ua.TotalTime,
                    IsFavorite = ua.IsFavorite,
                    IsPinned = ua.IsPinned
                })
                .ToList();
        }

    }
}