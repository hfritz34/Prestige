using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Prestige.Api.Data;
using Prestige.Api.Endpoints;
using Prestige.Api.Endpoints.Library.RequestResponse;
using Prestige.Api.Endpoints.Prestige.RequestResponse;
using Prestige.Api.Endpoints.Spotify.RequestResponse;
using Prestige.Api.Exceptions;
using Prestige.Api.Services;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;
using System.Linq;
using Prestige.Api.Configuration;

namespace Prestige.Api.Endpoints.Library
{
    public class LibraryServices : BaseService
    {
        private readonly RecentlyPlayedCosmosService _cosmosService;
        private readonly IDistributedCache _cache;
        private static readonly Dictionary<string, (DateTime expiry, RecentlyUpdatedResponse data)> _memoryCache = new();

        public LibraryServices(PrestigeContext db, ILogger<LibraryServices> logger, ClaimsPrincipal principal, IConfiguration config, RecentlyPlayedCosmosService cosmosService, IDistributedCache cache) 
            : base(db, logger, principal, config)
        {
            _cosmosService = cosmosService;
            _cache = cache;
        }

        public async Task<ItemDetailsResponse> GetItemDetailsAsync(string itemType, string itemId)
        {
            var userId = GetUserId();
            var normalizedType = itemType.ToLowerInvariant();

            switch (normalizedType)
            {
                case "track":
                    return await GetTrackDetailsAsync(userId, itemId);
                case "album":
                    return await GetAlbumDetailsAsync(userId, itemId);
                case "artist":
                    return await GetArtistDetailsAsync(userId, itemId);
                default:
                    throw new System.ArgumentException($"Invalid item type: {itemType}");
            }
        }

        public async Task<List<ItemDetailsResponse>> GetItemDetailsBatchAsync(List<ItemRequest> items)
        {
            var userId = GetUserId();
            
            // Group items by type for efficient queries
            var trackIds = items.Where(i => i.Type.ToLowerInvariant() == "track").Select(i => i.Id).ToList();
            var albumIds = items.Where(i => i.Type.ToLowerInvariant() == "album").Select(i => i.Id).ToList();
            var artistIds = items.Where(i => i.Type.ToLowerInvariant() == "artist").Select(i => i.Id).ToList();
            
            var tasks = new List<Task<List<ItemDetailsResponse>>>();
            
            if (trackIds.Any())
                tasks.Add(GetTrackDetailsBatchAsync(userId, trackIds));
            if (albumIds.Any())
                tasks.Add(GetAlbumDetailsBatchAsync(userId, albumIds));
            if (artistIds.Any())
                tasks.Add(GetArtistDetailsBatchAsync(userId, artistIds));
            
            var results = await Task.WhenAll(tasks);
            return results.SelectMany(r => r).ToList();
        }

        private string GetUserId()
        {
            return Principal.FindFirst(ClaimTypes.NameIdentifier)?.Value?.Split("|").Last() 
                ?? throw new UnauthorizedAccessException("User ID not found");
        }

        private async Task<ItemDetailsResponse> GetTrackDetailsAsync(string userId, string itemId)
        {
            // First try to find in user's tracks
            var userTrack = await PrestigeDb.UserTracks
                .AsNoTracking()  // Added for read-only query optimization
                .Include(ut => ut.Track)
                .ThenInclude(t => t.Album)
                .ThenInclude(a => a.Images)
                .Include(ut => ut.Track)
                .ThenInclude(t => t.Artists)
                .Where(ut => ut.User.Id == userId && ut.Track.Id == itemId)
                .FirstOrDefaultAsync();

            if (userTrack != null)
            {
                var track = userTrack.Track;
                return new ItemDetailsResponse
                {
                    Id = track.Id,
                    Name = track.Name,
                    ItemType = "track",
                    ImageUrl = track.Album?.Images?.FirstOrDefault()?.Url,
                    Artists = track.Artists?.Select(a => a.Name).ToList(),
                    AlbumName = track.Album?.Name
                };
            }

            // If not found in user tracks, try to find in general tracks table
            var track2 = await PrestigeDb.Tracks
                .AsNoTracking()  // Added for read-only query optimization
                .Include(t => t.Album)
                .ThenInclude(a => a.Images)
                .Include(t => t.Artists)
                .Where(t => t.Id == itemId)
                .FirstOrDefaultAsync();

            if (track2 != null)
            {
                return new ItemDetailsResponse
                {
                    Id = track2.Id,
                    Name = track2.Name,
                    ItemType = "track",
                    ImageUrl = track2.Album?.Images?.FirstOrDefault()?.Url,
                    Artists = track2.Artists?.Select(a => a.Name).ToList(),
                    AlbumName = track2.Album?.Name
                };
            }

            throw new NotFoundException(404, $"Track {itemId} not found");
        }

        private async Task<ItemDetailsResponse> GetAlbumDetailsAsync(string userId, string itemId)
        {
            // First try to find in user's albums
            var userAlbum = await PrestigeDb.UserAlbums
                .AsNoTracking()  // Added for read-only query optimization
                .Include(ua => ua.Album)
                .ThenInclude(a => a.Images)
                .Include(ua => ua.Album)
                .ThenInclude(a => a.Artists)
                .Where(ua => ua.User.Id == userId && ua.Album.Id == itemId)
                .FirstOrDefaultAsync();

            if (userAlbum != null)
            {
                var album = userAlbum.Album;
                return new ItemDetailsResponse
                {
                    Id = album.Id,
                    Name = album.Name,
                    ItemType = "album",
                    ImageUrl = album.Images?.FirstOrDefault()?.Url,
                    Artists = album.Artists?.Select(a => a.Name).ToList(),
                    AlbumName = null
                };
            }

            // If not found in user albums, try to find in general albums table
            var album2 = await PrestigeDb.Albums
                .AsNoTracking()  // Added for read-only query optimization
                .Include(a => a.Images)
                .Include(a => a.Artists)
                .Where(a => a.Id == itemId)
                .FirstOrDefaultAsync();

            if (album2 != null)
            {
                return new ItemDetailsResponse
                {
                    Id = album2.Id,
                    Name = album2.Name,
                    ItemType = "album",
                    ImageUrl = album2.Images?.FirstOrDefault()?.Url,
                    Artists = album2.Artists?.Select(a => a.Name).ToList(),
                    AlbumName = null
                };
            }

            throw new NotFoundException(404, $"Album {itemId} not found");
        }

        private async Task<ItemDetailsResponse> GetArtistDetailsAsync(string userId, string itemId)
        {
            // First try to find in user's artists
            var userArtist = await PrestigeDb.UserArtists
                .AsNoTracking()  // Added for read-only query optimization
                .Include(ua => ua.Artist)
                .ThenInclude(a => a.Images)
                .Where(ua => ua.User.Id == userId && ua.Artist.Id == itemId)
                .FirstOrDefaultAsync();

            if (userArtist != null)
            {
                var artist = userArtist.Artist;
                return new ItemDetailsResponse
                {
                    Id = artist.Id,
                    Name = artist.Name,
                    ItemType = "artist",
                    ImageUrl = artist.Images?.FirstOrDefault()?.Url,
                    Artists = null,
                    AlbumName = null
                };
            }

            // If not found in user artists, try to find in general artists table
            var artist2 = await PrestigeDb.Artists
                .AsNoTracking()  // Added for read-only query optimization
                .Include(a => a.Images)
                .Where(a => a.Id == itemId)
                .FirstOrDefaultAsync();

            if (artist2 != null)
            {
                return new ItemDetailsResponse
                {
                    Id = artist2.Id,
                    Name = artist2.Name,
                    ItemType = "artist",
                    ImageUrl = artist2.Images?.FirstOrDefault()?.Url,
                    Artists = null,
                    AlbumName = null
                };
            }

            throw new NotFoundException(404, $"Artist {itemId} not found");
        }

        private async Task<List<ItemDetailsResponse>> GetTrackDetailsBatchAsync(string userId, List<string> trackIds)
        {
            var results = new List<ItemDetailsResponse>();

            // First try to find in user's tracks
            var userTracks = await PrestigeDb.UserTracks
                .Include(ut => ut.Track)
                .ThenInclude(t => t.Album)
                .ThenInclude(a => a.Images)
                .Include(ut => ut.Track)
                .ThenInclude(t => t.Artists)
                .Where(ut => ut.User.Id == userId && trackIds.Contains(ut.Track.Id))
                .ToListAsync();

            foreach (var userTrack in userTracks)
            {
                var track = userTrack.Track;
                results.Add(new ItemDetailsResponse
                {
                    Id = track.Id,
                    Name = track.Name,
                    ItemType = "track",
                    ImageUrl = track.Album?.Images?.FirstOrDefault()?.Url,
                    Artists = track.Artists?.Select(a => a.Name).ToList(),
                    AlbumName = track.Album?.Name
                });
            }

            // Find remaining tracks not in user's collection
            var foundIds = userTracks.Select(ut => ut.Track.Id).ToList();
            var remainingIds = trackIds.Except(foundIds).ToList();

            if (remainingIds.Any())
            {
                var generalTracks = await PrestigeDb.Tracks
                    .Include(t => t.Album)
                    .ThenInclude(a => a.Images)
                    .Include(t => t.Artists)
                    .Where(t => remainingIds.Contains(t.Id))
                    .ToListAsync();

                foreach (var track in generalTracks)
                {
                    results.Add(new ItemDetailsResponse
                    {
                        Id = track.Id,
                        Name = track.Name,
                        ItemType = "track",
                        ImageUrl = track.Album?.Images?.FirstOrDefault()?.Url,
                        Artists = track.Artists?.Select(a => a.Name).ToList(),
                        AlbumName = track.Album?.Name
                    });
                }
            }

            return results;
        }

        private async Task<List<ItemDetailsResponse>> GetAlbumDetailsBatchAsync(string userId, List<string> albumIds)
        {
            var results = new List<ItemDetailsResponse>();

            // First try to find in user's albums
            var userAlbums = await PrestigeDb.UserAlbums
                .Include(ua => ua.Album)
                .ThenInclude(a => a.Images)
                .Include(ua => ua.Album)
                .ThenInclude(a => a.Artists)
                .Where(ua => ua.User.Id == userId && albumIds.Contains(ua.Album.Id))
                .ToListAsync();

            foreach (var userAlbum in userAlbums)
            {
                var album = userAlbum.Album;
                results.Add(new ItemDetailsResponse
                {
                    Id = album.Id,
                    Name = album.Name,
                    ItemType = "album",
                    ImageUrl = album.Images?.FirstOrDefault()?.Url,
                    Artists = album.Artists?.Select(a => a.Name).ToList(),
                    AlbumName = null
                });
            }

            // Find remaining albums not in user's collection
            var foundIds = userAlbums.Select(ua => ua.Album.Id).ToList();
            var remainingIds = albumIds.Except(foundIds).ToList();

            if (remainingIds.Any())
            {
                var generalAlbums = await PrestigeDb.Albums
                    .Include(a => a.Images)
                    .Include(a => a.Artists)
                    .Where(a => remainingIds.Contains(a.Id))
                    .ToListAsync();

                foreach (var album in generalAlbums)
                {
                    results.Add(new ItemDetailsResponse
                    {
                        Id = album.Id,
                        Name = album.Name,
                        ItemType = "album",
                        ImageUrl = album.Images?.FirstOrDefault()?.Url,
                        Artists = album.Artists?.Select(a => a.Name).ToList(),
                        AlbumName = null
                    });
                }
            }

            return results;
        }

        private async Task<List<ItemDetailsResponse>> GetArtistDetailsBatchAsync(string userId, List<string> artistIds)
        {
            var results = new List<ItemDetailsResponse>();

            // First try to find in user's artists
            var userArtists = await PrestigeDb.UserArtists
                .Include(ua => ua.Artist)
                .ThenInclude(a => a.Images)
                .Where(ua => ua.User.Id == userId && artistIds.Contains(ua.Artist.Id))
                .ToListAsync();

            foreach (var userArtist in userArtists)
            {
                var artist = userArtist.Artist;
                results.Add(new ItemDetailsResponse
                {
                    Id = artist.Id,
                    Name = artist.Name,
                    ItemType = "artist",
                    ImageUrl = artist.Images?.FirstOrDefault()?.Url,
                    Artists = null,
                    AlbumName = null
                });
            }

            // Find remaining artists not in user's collection
            var foundIds = userArtists.Select(ua => ua.Artist.Id).ToList();
            var remainingIds = artistIds.Except(foundIds).ToList();

            if (remainingIds.Any())
            {
                var generalArtists = await PrestigeDb.Artists
                    .Include(a => a.Images)
                    .Where(a => remainingIds.Contains(a.Id))
                    .ToListAsync();

                foreach (var artist in generalArtists)
                {
                    results.Add(new ItemDetailsResponse
                    {
                        Id = artist.Id,
                        Name = artist.Name,
                        ItemType = "artist",
                        ImageUrl = artist.Images?.FirstOrDefault()?.Url,
                        Artists = null,
                        AlbumName = null
                    });
                }
            }

            return results;
        }

        public async Task<RecentlyUpdatedResponse> GetRecentlyUpdatedAsync(string userId, DateTime since)
        {
            var currentUserId = GetUserId();
            if (userId != currentUserId)
            {
                _logger.LogWarning($"Unauthorized access attempt. Requested user ID: {userId}, Current user ID: {currentUserId}");
                throw new UnauthorizedAccessException($"User {userId} is not authorized to access this resource");
            }

            try
            {
                // Try memory cache first
                var cacheKey = $"recently_updated:{userId}";
                if (_memoryCache.TryGetValue(cacheKey, out var cached) && cached.expiry > DateTime.UtcNow)
                {
                    _logger.LogInformation($"Returning memory cached recently updated data for user {userId}");
                    return cached.data;
                }

                // Try distributed cache as fallback
                string? cachedData = null;
                try
                {
                    cachedData = await _cache.GetStringAsync(cacheKey);
                }
                catch (System.Exception cacheEx)
                {
                    _logger.LogWarning(cacheEx, $"Failed to get distributed cached data for user {userId}");
                }
                
                if (!string.IsNullOrEmpty(cachedData))
                {
                    _logger.LogInformation($"Returning distributed cached recently updated data for user {userId}");
                    var deserializedData = JsonSerializer.Deserialize<RecentlyUpdatedResponse>(cachedData);
                    if (deserializedData != null && (deserializedData.Tracks.Any() || deserializedData.Albums.Any() || deserializedData.Artists.Any()))
                    {
                        return deserializedData;
                    }
                }

                // Query database for recently updated items
                _logger.LogInformation($"Querying database for recently updated items for user {userId} since {since}");
                
                // First check if user has any tracks at all
                var totalUserTracks = await PrestigeDb.UserTracks.CountAsync(ut => ut.User.Id == userId);
                _logger.LogInformation($"User {userId} has {totalUserTracks} total tracks in database");
                
                // Get recently updated tracks
                var recentTracks = await PrestigeDb.UserTracks
                    .Where(ut => ut.User.Id == userId && ut.LastUpdatedAt >= since)
                    .Include(ut => ut.Track)
                        .ThenInclude(t => t.Album)
                            .ThenInclude(a => a.Images)
                    .Include(ut => ut.Track)
                        .ThenInclude(t => t.Artists)
                            .ThenInclude(ar => ar.Images)
                    .Include(ut => ut.Track.Album.Artists)
                        .ThenInclude(ar => ar.Images)
                    .Include(ut => ut.User)
                    .OrderByDescending(ut => ut.LastUpdatedAt)
                    .Take(60)
                    .ToListAsync();

                // Get recently updated albums
                var recentAlbums = await PrestigeDb.UserAlbums
                    .Where(ua => ua.User.Id == userId && ua.LastUpdatedAt >= since)
                    .Include(ua => ua.Album)
                        .ThenInclude(a => a.Images)
                    .Include(ua => ua.Album)
                        .ThenInclude(a => a.Artists)
                            .ThenInclude(ar => ar.Images)
                    .Include(ua => ua.User)
                    .OrderByDescending(ua => ua.LastUpdatedAt)
                    .Take(60)
                    .ToListAsync();

                // Get recently updated artists
                var recentArtists = await PrestigeDb.UserArtists
                    .Where(ua => ua.User.Id == userId && ua.LastUpdatedAt >= since)
                    .Include(ua => ua.Artist)
                        .ThenInclude(a => a.Images)
                    .Include(ua => ua.User)
                    .OrderByDescending(ua => ua.LastUpdatedAt)
                    .Take(60)
                    .ToListAsync();

                // Convert to response DTOs
                var response = new RecentlyUpdatedResponse
                {
                    Tracks = recentTracks.Select(ut => new UserTrackResponse
                    {
                        Track = new TrackResponse(ut.Track),
                        TotalTime = ut.TotalTime,
                        UserId = ut.User.Id,
                        IsFavorite = ut.IsFavorite,
                        IsPinned = ut.IsPinned,
                        PrestigeTier = PrestigeThresholds.CalculatePrestigeTier(ut.TotalTime, "track")
                    }).ToList(),

                    Albums = recentAlbums.Select(ua => new UserAlbumResponse
                    {
                        Album = new AlbumResponse(ua.Album),
                        TotalTime = ua.TotalTime,
                        UserId = ua.User.Id,
                        IsFavorite = ua.IsFavorite,
                        IsPinned = ua.IsPinned,
                        PrestigeTier = PrestigeThresholds.CalculatePrestigeTier(ua.TotalTime, "album")
                    }).ToList(),

                    Artists = recentArtists.Select(ua => new UserArtistResponse
                    {
                        Artist = new ArtistResponse(ua.Artist),
                        TotalTime = ua.TotalTime,
                        UserId = ua.User.Id,
                        IsFavorite = ua.IsFavorite,
                        IsPinned = ua.IsPinned,
                        PrestigeTier = PrestigeThresholds.CalculatePrestigeTier(ua.TotalTime, "artist")
                    }).ToList()
                };

                // Cache the result for 1 hour
                _memoryCache[cacheKey] = (DateTime.UtcNow.AddHours(1), response);
                
                // Try to cache in distributed cache as well
                try
                {
                    var cacheOptions = new Microsoft.Extensions.Caching.Distributed.DistributedCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1)
                    };
                    await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(response), cacheOptions);
                }
                catch (System.Exception cacheEx)
                {
                    _logger.LogWarning(cacheEx, $"Failed to set distributed cache for user {userId}");
                }

                _logger.LogInformation($"Found {response.Tracks.Count} tracks, {response.Albums.Count} albums, {response.Artists.Count} artists updated since {since} for user {userId}");
                return response;
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, $"Error getting recently updated items for user {userId}");
                return new RecentlyUpdatedResponse();
            }
        }

        public async Task ProcessRecentlyPlayedBatchUnauthenticatedAsync(BatchUpdateRequest request)
        {
            try
            {
                _logger.LogInformation($"Processing batch {request.BatchId} with {request.Items.Count} items");

                if (request?.Items == null || !request.Items.Any())
                {
                    _logger.LogWarning($"Batch {request?.BatchId} has no items to process");
                    return;
                }

                // Group items by user
                var userGroups = request.Items.GroupBy(i => i.UserId).ToList();
                
                foreach (var userGroup in userGroups)
                {
                    var userId = userGroup.Key;
                    var userItems = userGroup.ToList();
                    
                    _logger.LogInformation($"Processing {userItems.Count} items for user {userId}");

                    // Get unique track IDs for this user
                    var trackIds = userItems.Select(i => i.TrackId).Distinct().ToList();
                    _logger.LogInformation($"Found {trackIds.Count} unique track IDs for user {userId}: {string.Join(", ", trackIds)}");

                    // Query SQL database for these tracks and related data
                    _logger.LogDebug($"Querying UserTracks for user {userId}");
                    var userTracks = await PrestigeDb.UserTracks
                        .Where(ut => ut.User.Id == userId && trackIds.Contains(ut.Track.Id))
                        .Include(ut => ut.Track)
                            .ThenInclude(t => t.Album)
                                .ThenInclude(a => a.Images)
                        .Include(ut => ut.Track)
                            .ThenInclude(t => t.Artists)
                                .ThenInclude(ar => ar.Images)
                        .Include(ut => ut.Track.Album.Artists)
                            .ThenInclude(ar => ar.Images)
                        .Include(ut => ut.User)
                        .Take(60) // Limit as requested
                        .ToListAsync();

                    // Get unique album IDs from the tracks
                    var albumIds = userTracks.Select(ut => ut.Track.Album?.Id).Where(id => id != null).Distinct().ToList();
                    var userAlbums = await PrestigeDb.UserAlbums
                        .Where(ua => ua.User.Id == userId && albumIds.Contains(ua.Album.Id))
                        .Include(ua => ua.Album)
                            .ThenInclude(a => a.Images)
                        .Include(ua => ua.Album)
                            .ThenInclude(a => a.Artists)
                                .ThenInclude(ar => ar.Images)
                        .Include(ua => ua.User)
                        .Take(60)
                        .ToListAsync();

                    // Get unique artist IDs from the tracks
                    var artistIds = userTracks.SelectMany(ut => ut.Track.Artists?.Select(a => a.Id) ?? new List<string>()).Distinct().ToList();
                    var userArtists = await PrestigeDb.UserArtists
                        .Where(ua => ua.User.Id == userId && artistIds.Contains(ua.Artist.Id))
                        .Include(ua => ua.Artist)
                            .ThenInclude(a => a.Images)
                        .Include(ua => ua.User)
                        .Take(60)
                        .ToListAsync();

                    // Convert to response DTOs
                    var recentlyUpdatedResponse = new RecentlyUpdatedResponse
                    {
                        Tracks = userTracks.Select(ut => new UserTrackResponse
                        {
                            Track = new TrackResponse(ut.Track),
                            TotalTime = ut.TotalTime,
                            UserId = ut.User.Id,
                            IsFavorite = ut.IsFavorite,
                            IsPinned = ut.IsPinned,
                            PrestigeTier = PrestigeThresholds.CalculatePrestigeTier(ut.TotalTime, "track")
                        }).ToList(),

                        Albums = userAlbums.Select(ua => new UserAlbumResponse
                        {
                            Album = new AlbumResponse(ua.Album),
                            TotalTime = ua.TotalTime,
                            UserId = ua.User.Id,
                            IsFavorite = ua.IsFavorite,
                            IsPinned = ua.IsPinned,
                            PrestigeTier = PrestigeThresholds.CalculatePrestigeTier(ua.TotalTime, "album")
                        }).ToList(),

                        Artists = userArtists.Select(ua => new UserArtistResponse
                        {
                            Artist = new ArtistResponse(ua.Artist),
                            TotalTime = ua.TotalTime,
                            UserId = ua.User.Id,
                            IsFavorite = ua.IsFavorite,
                            IsPinned = ua.IsPinned,
                            PrestigeTier = PrestigeThresholds.CalculatePrestigeTier(ua.TotalTime, "artist")
                        }).ToList()
                    };

                    // Cache the result for 2 hours (until next potential batch)
                    var cacheKey = $"recently_updated:{userId}";
                    
                    // Store in memory cache first (always works)
                    _memoryCache[cacheKey] = (DateTime.UtcNow.AddHours(2), recentlyUpdatedResponse);
                    _logger.LogInformation($"Memory cached recently updated data for user {userId}: {recentlyUpdatedResponse.Tracks.Count} tracks, {recentlyUpdatedResponse.Albums.Count} albums, {recentlyUpdatedResponse.Artists.Count} artists");
                    
                    // Try distributed cache as well
                    try
                    {
                        var serializedData = JsonSerializer.Serialize(recentlyUpdatedResponse);
                        var cacheOptions = new DistributedCacheEntryOptions
                        {
                            AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(2)
                        };

                        await _cache.SetStringAsync(cacheKey, serializedData, cacheOptions);
                        _logger.LogInformation($"Distributed cached recently updated data for user {userId}");
                    }
                    catch (System.Exception cacheEx)
                    {
                        _logger.LogWarning(cacheEx, $"Failed to distributed cache data for user {userId}, using memory cache only");
                    }
                }
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, $"Error processing recently played batch {request.BatchId}");
                throw;
            }
        }
    }
}