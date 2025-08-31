using Hangfire;
using Microsoft.EntityFrameworkCore;
using Prestige.Api.Data;
using Prestige.Api.Domain;

namespace Prestige.Api.Services
{
    public class RatingBackgroundJobs
    {
        private readonly PrestigeContext _context;
        private readonly ILogger<RatingBackgroundJobs> _logger;
        private readonly PrestigeCacheService _cacheService;
        
        public RatingBackgroundJobs(PrestigeContext context, ILogger<RatingBackgroundJobs> logger, PrestigeCacheService cacheService)
        {
            _context = context;
            _logger = logger;
            _cacheService = cacheService;
        }
        
        [AutomaticRetry(Attempts = 3)]
        public async Task RecalculateUserStatisticsAsync(string userId)
        {
            try
            {
                _logger.LogInformation("Starting statistics recalculation for user {UserId}", userId);
                
                // Get all user ratings
                var ratings = await _context.Ratings
                    .Where(r => r.User.Id == userId)
                    .Include(r => r.Category)
                    .ToListAsync();
                
                if (!ratings.Any())
                {
                    _logger.LogInformation("No ratings found for user {UserId}", userId);
                    return;
                }
                
                // Group by item type and calculate statistics
                var ratingsByType = ratings.GroupBy(r => r.ItemType);
                
                foreach (var typeGroup in ratingsByType)
                {
                    var itemType = typeGroup.Key;
                    var typeRatings = typeGroup.ToList();
                    
                    // Calculate average score
                    var averageScore = typeRatings.Average(r => r.PersonalScore);
                    
                    // Find top rated items
                    var topRated = typeRatings
                        .OrderByDescending(r => r.PersonalScore)
                        .Take(10)
                        .ToList();
                    
                    _logger.LogDebug("User {UserId} {ItemType} stats: Avg={AvgScore:F2}, Top={TopCount}", 
                        userId, itemType, averageScore, topRated.Count);
                }
                
                // Invalidate cache for this user
                await _cacheService.InvalidateUserRatingsAsync(userId);
                await _cacheService.InvalidateRatingStatisticsAsync(userId);
                
                _logger.LogInformation("Completed statistics recalculation for user {UserId}", userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error recalculating statistics for user {UserId}", userId);
                throw;
            }
        }
        
        [AutomaticRetry(Attempts = 5)]
        [DisableConcurrentExecution(timeoutInSeconds: 300)]
        public async Task SyncSpotifyDataAsync(string userId)
        {
            try
            {
                _logger.LogInformation("Starting Spotify data sync for user {UserId}", userId);
                
                // Get user with tokens
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
                if (user == null)
                {
                    _logger.LogWarning("User {UserId} not found for Spotify sync", userId);
                    return;
                }
                
                if (string.IsNullOrEmpty(user.AccessToken))
                {
                    _logger.LogWarning("User {UserId} has no Spotify access token", userId);
                    return;
                }
                
                // Check if token is expired
                if (user.ExpiresAt <= DateTime.UtcNow)
                {
                    _logger.LogInformation("Access token expired for user {UserId}, needs refresh", userId);
                    // Token refresh would happen here in a real implementation
                    return;
                }
                
                // Simulate Spotify API calls (in real implementation, this would call Spotify API)
                await Task.Delay(1000); // Simulate API call delay
                
                _logger.LogInformation("Completed Spotify data sync for user {UserId}", userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error syncing Spotify data for user {UserId}", userId);
                throw;
            }
        }
        
        [DisableConcurrentExecution(timeoutInSeconds: 60)]
        public async Task CleanupOldComparisonsAsync()
        {
            try
            {
                _logger.LogInformation("Starting cleanup of old rating comparisons");
                
                var cutoffDate = DateTime.UtcNow.AddDays(-30); // Keep comparisons for 30 days
                
                var oldComparisons = await _context.RatingComparisons
                    .Where(rc => rc.ComparisonDate < cutoffDate)
                    .ToListAsync();
                
                if (oldComparisons.Any())
                {
                    _context.RatingComparisons.RemoveRange(oldComparisons);
                    var deleted = await _context.SaveChangesAsync();
                    
                    _logger.LogInformation("Cleaned up {Count} old rating comparisons", deleted);
                }
                else
                {
                    _logger.LogInformation("No old rating comparisons to clean up");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cleaning up old rating comparisons");
                throw;
            }
        }
        
        [DisableConcurrentExecution(timeoutInSeconds: 300)]
        public async Task OptimizeDatabaseAsync()
        {
            try
            {
                _logger.LogInformation("Starting database optimization tasks");
                
                // Update statistics on key tables (this would be SQL Server specific commands)
                await _context.Database.ExecuteSqlRawAsync("UPDATE STATISTICS Ratings");
                await _context.Database.ExecuteSqlRawAsync("UPDATE STATISTICS UserTrack");
                await _context.Database.ExecuteSqlRawAsync("UPDATE STATISTICS UserAlbum");
                await _context.Database.ExecuteSqlRawAsync("UPDATE STATISTICS UserArtist");
                
                _logger.LogInformation("Completed database optimization tasks");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during database optimization");
                throw;
            }
        }
        
        [AutomaticRetry(Attempts = 2)]
        public async Task ProcessRatingUpdateAsync(string userId, string itemType, string itemId)
        {
            try
            {
                _logger.LogInformation("Processing rating update for user {UserId}, item {ItemType}:{ItemId}", 
                    userId, itemType, itemId);
                
                // Recalculate position-based scores for affected items
                var relatedRatings = await _context.Ratings
                    .Where(r => r.User.Id == userId && r.ItemType == itemType)
                    .OrderBy(r => r.Position)
                    .ToListAsync();
                
                // Update scores based on position (this is a simplified example)
                for (int i = 0; i < relatedRatings.Count; i++)
                {
                    var rating = relatedRatings[i];
                    var newScore = CalculateScoreFromPosition(i + 1, relatedRatings.Count);
                    
                    if (Math.Abs(rating.PersonalScore - newScore) > 0.01m)
                    {
                        rating.UpdateScore(newScore);
                    }
                }
                
                await _context.SaveChangesAsync();
                
                // Invalidate cache
                await _cacheService.InvalidateUserRatingsAsync(userId, itemType);
                
                _logger.LogInformation("Completed rating update processing for user {UserId}", userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing rating update for user {UserId}", userId);
                throw;
            }
        }
        
        private static decimal CalculateScoreFromPosition(int position, int totalCount)
        {
            // Simple linear scoring: top position = 100, bottom = 0
            if (totalCount <= 1) return 100m;
            
            var percentage = (decimal)(totalCount - position) / (totalCount - 1);
            return Math.Round(percentage * 100, 2);
        }
    }
}