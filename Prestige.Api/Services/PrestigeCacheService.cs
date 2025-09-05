using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Prestige.Api.Domain;
using Prestige.Api.Endpoints.Library.RequestResponse;
using Prestige.Api.Endpoints.Rating.RequestResponse;

namespace Prestige.Api.Services
{
    public class PrestigeCacheService
    {
        private readonly IDistributedCache _cache;
        private readonly ILogger<PrestigeCacheService> _logger;
        
        // Updated cache times to match frontend optimizations
        private const int UserRatingsCacheMinutes = 60;        // Was 30, now 1 hour
        private const int ItemMetadataCacheHours = 6;          // Was 24, now 6 hours (metadata rarely changes)
        private const int SpotifyDataCacheMinutes = 30;        // Was 15, now 30 minutes
        private const int RatingCategoriesCacheHours = 48;     // Unchanged - categories are very stable
        private const int FriendDataCacheMinutes = 60;         // New: 1 hour for friend data
        private const int UserProfileCacheMinutes = 120;       // New: 2 hours for user profiles
        private const int PrestigeCalculationCacheMinutes = 5; // New: 5 minutes for prestige calculations
        
        public PrestigeCacheService(IDistributedCache cache, ILogger<PrestigeCacheService> logger)
        {
            _cache = cache;
            _logger = logger;
        }
        
        // User Ratings Cache
        public async Task<List<RatingResponse>?> GetUserRatingsAsync(string userId, string itemType, int? categoryId = null)
        {
            var cacheKey = categoryId.HasValue 
                ? $"user_ratings:{userId}:{itemType}:{categoryId}" 
                : $"user_ratings:{userId}:{itemType}";
                
            try
            {
                var cached = await _cache.GetStringAsync(cacheKey);
                if (!string.IsNullOrEmpty(cached))
                {
                    _logger.LogDebug("Cache hit for user ratings: {CacheKey}", cacheKey);
                    return JsonSerializer.Deserialize<List<RatingResponse>>(cached);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error reading from cache for key: {CacheKey}", cacheKey);
            }
            
            return null;
        }
        
        public async Task SetUserRatingsAsync(string userId, string itemType, List<RatingResponse> ratings, int? categoryId = null)
        {
            var cacheKey = categoryId.HasValue 
                ? $"user_ratings:{userId}:{itemType}:{categoryId}" 
                : $"user_ratings:{userId}:{itemType}";
                
            try
            {
                var serialized = JsonSerializer.Serialize(ratings);
                await _cache.SetStringAsync(cacheKey, serialized, new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(UserRatingsCacheMinutes)
                });
                _logger.LogDebug("Cached user ratings: {CacheKey}", cacheKey);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error writing to cache for key: {CacheKey}", cacheKey);
            }
        }
        
        // Item Metadata Cache
        public async Task<ItemDetailsResponse?> GetItemMetadataAsync(string itemType, string itemId)
        {
            var cacheKey = $"item_metadata:{itemType}:{itemId}";
            
            try
            {
                var cached = await _cache.GetStringAsync(cacheKey);
                if (!string.IsNullOrEmpty(cached))
                {
                    _logger.LogDebug("Cache hit for item metadata: {CacheKey}", cacheKey);
                    return JsonSerializer.Deserialize<ItemDetailsResponse>(cached);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error reading from cache for key: {CacheKey}", cacheKey);
            }
            
            return null;
        }
        
        public async Task SetItemMetadataAsync(string itemType, string itemId, ItemDetailsResponse metadata)
        {
            var cacheKey = $"item_metadata:{itemType}:{itemId}";
            
            try
            {
                var serialized = JsonSerializer.Serialize(metadata);
                await _cache.SetStringAsync(cacheKey, serialized, new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(ItemMetadataCacheHours)
                });
                _logger.LogDebug("Cached item metadata: {CacheKey}", cacheKey);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error writing to cache for key: {CacheKey}", cacheKey);
            }
        }
        
        // Batch Metadata Cache
        public async Task<Dictionary<string, ItemDetailsResponse>> GetBatchMetadataAsync(List<ItemRequest> items)
        {
            var result = new Dictionary<string, ItemDetailsResponse>();
            
            foreach (var item in items)
            {
                var metadata = await GetItemMetadataAsync(item.Type, item.Id);
                if (metadata != null)
                {
                    result[$"{item.Type}:{item.Id}"] = metadata;
                }
            }
            
            return result;
        }
        
        public async Task SetBatchMetadataAsync(List<ItemDetailsResponse> items)
        {
            var tasks = items.Select(item => SetItemMetadataAsync(item.ItemType, item.Id, item));
            await Task.WhenAll(tasks);
        }
        
        // Rating Categories Cache
        public async Task<List<RatingCategoryResponse>?> GetRatingCategoriesAsync()
        {
            var cacheKey = "rating_categories:all";
            
            try
            {
                var cached = await _cache.GetStringAsync(cacheKey);
                if (!string.IsNullOrEmpty(cached))
                {
                    _logger.LogDebug("Cache hit for rating categories");
                    return JsonSerializer.Deserialize<List<RatingCategoryResponse>>(cached);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error reading rating categories from cache");
            }
            
            return null;
        }
        
        public async Task SetRatingCategoriesAsync(List<RatingCategoryResponse> categories)
        {
            var cacheKey = "rating_categories:all";
            
            try
            {
                var serialized = JsonSerializer.Serialize(categories);
                await _cache.SetStringAsync(cacheKey, serialized, new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(RatingCategoriesCacheHours)
                });
                _logger.LogDebug("Cached rating categories");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error caching rating categories");
            }
        }
        
        // Cache Invalidation
        public async Task InvalidateUserRatingsAsync(string userId, string? itemType = null)
        {
            try
            {
                if (itemType != null)
                {
                    // Invalidate specific item type ratings
                    var patterns = new[]
                    {
                        $"user_ratings:{userId}:{itemType}",
                        $"user_ratings:{userId}:{itemType}:*"
                    };
                    
                    foreach (var pattern in patterns)
                    {
                        await _cache.RemoveAsync(pattern);
                    }
                }
                else
                {
                    // Invalidate all user ratings
                    var itemTypes = new[] { "track", "album", "artist" };
                    foreach (var type in itemTypes)
                    {
                        await InvalidateUserRatingsAsync(userId, type);
                    }
                }
                
                _logger.LogDebug("Invalidated user ratings cache for user: {UserId}, itemType: {ItemType}", userId, itemType);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error invalidating user ratings cache");
            }
        }
        
        public async Task InvalidateRatingStatisticsAsync(string userId)
        {
            try
            {
                var cacheKey = $"rating_statistics:{userId}";
                await _cache.RemoveAsync(cacheKey);
                _logger.LogDebug("Invalidated rating statistics cache for user: {UserId}", userId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error invalidating rating statistics cache");
            }
        }
        
        public async Task InvalidateItemMetadataAsync(string itemType, string itemId)
        {
            try
            {
                var cacheKey = $"item_metadata:{itemType}:{itemId}";
                await _cache.RemoveAsync(cacheKey);
                _logger.LogDebug("Invalidated item metadata cache: {CacheKey}", cacheKey);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error invalidating item metadata cache");
            }
        }
        
        // Prestige Calculation Cache
        public async Task<string?> GetPrestigeCalculationAsync(string userId, string itemType, string itemId)
        {
            var cacheKey = $"prestige:{userId}:{itemType}:{itemId}";
            
            try
            {
                var cached = await _cache.GetStringAsync(cacheKey);
                if (!string.IsNullOrEmpty(cached))
                {
                    _logger.LogDebug("Cache hit for prestige calculation: {CacheKey}", cacheKey);
                    return cached;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error reading prestige from cache for key: {CacheKey}", cacheKey);
            }
            
            return null;
        }
        
        public async Task SetPrestigeCalculationAsync(string userId, string itemType, string itemId, string prestigeTier)
        {
            var cacheKey = $"prestige:{userId}:{itemType}:{itemId}";
            
            try
            {
                await _cache.SetStringAsync(cacheKey, prestigeTier, new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(PrestigeCalculationCacheMinutes)
                });
                _logger.LogDebug("Cached prestige calculation: {CacheKey}", cacheKey);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error writing prestige to cache for key: {CacheKey}", cacheKey);
            }
        }
        
        // Friend Data Cache
        public async Task<T?> GetFriendDataAsync<T>(string cacheKey) where T : class
        {
            try
            {
                var cached = await _cache.GetStringAsync(cacheKey);
                if (!string.IsNullOrEmpty(cached))
                {
                    _logger.LogDebug("Cache hit for friend data: {CacheKey}", cacheKey);
                    return JsonSerializer.Deserialize<T>(cached);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error reading friend data from cache for key: {CacheKey}", cacheKey);
            }
            
            return null;
        }
        
        public async Task SetFriendDataAsync<T>(string cacheKey, T data) where T : class
        {
            try
            {
                var serialized = JsonSerializer.Serialize(data);
                await _cache.SetStringAsync(cacheKey, serialized, new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(FriendDataCacheMinutes)
                });
                _logger.LogDebug("Cached friend data: {CacheKey}", cacheKey);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error writing friend data to cache for key: {CacheKey}", cacheKey);
            }
        }
        
        // User Profile Cache
        public async Task<T?> GetUserProfileAsync<T>(string userId) where T : class
        {
            var cacheKey = $"user_profile:{userId}";
            
            try
            {
                var cached = await _cache.GetStringAsync(cacheKey);
                if (!string.IsNullOrEmpty(cached))
                {
                    _logger.LogDebug("Cache hit for user profile: {CacheKey}", cacheKey);
                    return JsonSerializer.Deserialize<T>(cached);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error reading user profile from cache for key: {CacheKey}", cacheKey);
            }
            
            return null;
        }
        
        public async Task SetUserProfileAsync<T>(string userId, T profile) where T : class
        {
            var cacheKey = $"user_profile:{userId}";
            
            try
            {
                var serialized = JsonSerializer.Serialize(profile);
                await _cache.SetStringAsync(cacheKey, serialized, new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(UserProfileCacheMinutes)
                });
                _logger.LogDebug("Cached user profile: {CacheKey}", cacheKey);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error writing user profile to cache for key: {CacheKey}", cacheKey);
            }
        }
    }
}