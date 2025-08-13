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
        
        private const int UserRatingsCacheMinutes = 30;
        private const int ItemMetadataCacheHours = 24;
        private const int SpotifyDataCacheMinutes = 15;
        private const int RatingCategoriesCacheHours = 48;
        
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
    }
}