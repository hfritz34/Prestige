using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Prestige.Api.Data;
using Prestige.Api.Domain;
using Prestige.Api.Endpoints.Rating.RequestResponse;
using Prestige.Api.Exceptions;

namespace Prestige.Api.Endpoints.Rating
{
    public class RatingServices : BaseService
    {
        public RatingServices(PrestigeContext db, ILogger<RatingServices> logger, ClaimsPrincipal principal, IConfiguration config) 
            : base(db, logger, principal, config)
        {
        }

        public async Task<IEnumerable<RatingCategoryResponse>> GetRatingCategoriesAsync()
        {
            var categories = await PrestigeDb.RatingCategories
                .OrderBy(c => c.DisplayOrder)
                .Select(c => new RatingCategoryResponse
                {
                    Id = c.Id,
                    Name = c.Name,
                    MinScore = c.MinScore,
                    MaxScore = c.MaxScore,
                    ColorHex = c.ColorHex,
                    DisplayOrder = c.DisplayOrder
                })
                .ToListAsync();

            return categories;
        }

        public async Task<RatingResponse> StartRatingAsync(string itemType, string itemId)
        {
            var userId = GetUserId();
            var normalizedType = itemType.ToLowerInvariant();
            var user = await GetUserAsync(userId);

            // Check if item already rated
            var existingRating = await PrestigeDb.Ratings
                .Include(r => r.Category)
                .FirstOrDefaultAsync(r => r.User.Id == userId && r.ItemId == itemId && r.ItemType.ToLower() == normalizedType);

            if (existingRating != null)
            {
                return new RatingResponse
                {
                    ItemId = itemId,
                    ItemType = normalizedType,
                    CategoryId = existingRating.Category.Id,
                    PersonalScore = existingRating.PersonalScore,
                    Position = existingRating.Position,
                    IsNewRating = false
                };
            }

            return new RatingResponse
            {
                ItemId = itemId,
                ItemType = normalizedType,
                IsNewRating = true
            };
        }

        public async Task<ComparisonResultResponse> SubmitComparisonAsync(ComparisonRequest request)
        {
            var userId = GetUserId();
            var user = await GetUserAsync(userId);
            var normalizedType = request.ItemType.ToLowerInvariant();

            // Record the comparison
            var comparison = new RatingComparison(user, request.ItemId1, request.ItemId2, normalizedType, request.WinnerId);
            PrestigeDb.RatingComparisons.Add(comparison);

            // TODO: Implement binary search logic to determine position and score

            await PrestigeDb.SaveChangesAsync();

            return new ComparisonResultResponse
            {
                Success = true,
                Message = "Comparison recorded successfully"
            };
        }

        public async Task<IEnumerable<RatingResponse>> GetUserRatingsAsync(string itemType)
        {
            var userId = GetUserId();
            var normalizedType = itemType.ToLowerInvariant();
            
            var ratings = await PrestigeDb.Ratings
                .Include(r => r.Category)
                .Where(r => r.User.Id == userId && r.ItemType.ToLower() == normalizedType)
                .Select(r => new RatingResponse
                {
                    ItemId = r.ItemId,
                    ItemType = r.ItemType,
                    CategoryId = r.Category.Id,
                    PersonalScore = r.PersonalScore,
                    Position = r.Position,
                    AlbumId = r.AlbumId, // Now directly from the Rating entity
                    IsNewRating = false
                })
                .ToListAsync();

            return ratings;
        }

        public async Task<RatingResponse> SaveRatingAsync(string itemType, string itemId, decimal personalScore, int categoryId)
        {
            var userId = GetUserId();
            var normalizedType = itemType.ToLowerInvariant();
            var user = await GetUserAsync(userId);
            var category = await PrestigeDb.RatingCategories.FindAsync(categoryId)
                ?? throw new NotFoundException(404, $"Category {categoryId} not found");

            // Get album ID if this is a track
            string? albumId = null;
            if (normalizedType == "track")
            {
                var track = await PrestigeDb.Tracks
                    .Include(t => t.Album)
                    .FirstOrDefaultAsync(t => t.Id == itemId);
                albumId = track?.Album?.Id;
            }

            // Check if rating already exists
            var existingRating = await PrestigeDb.Ratings
                .FirstOrDefaultAsync(r => r.User.Id == userId && r.ItemId == itemId && r.ItemType.ToLower() == normalizedType);

            bool isNewRating = existingRating == null;

            // personalScore is actually the position from frontend (0 = best, higher = worse)
            var position = (int)personalScore;
            
            if (existingRating != null)
            {
                // Update existing rating with new position
                existingRating.UpdateRating(0, category, position); // Score will be calculated later
            }
            else
            {
                // Create new rating with position and album ID
                var newRating = new Domain.Rating(user, itemId, normalizedType, category, position, 0, albumId);
                PrestigeDb.Ratings.Add(newRating);
            }

            await PrestigeDb.SaveChangesAsync();

            // Recalculate all scores based on new positions
            await RecalculateUserScoresWithPositionAsync(userId, normalizedType, itemId, position);

            // Get the final position and score for the saved item
            var savedRating = await PrestigeDb.Ratings
                .FirstOrDefaultAsync(r => r.User.Id == userId && r.ItemId == itemId && r.ItemType.ToLower() == normalizedType);

            return new RatingResponse
            {
                ItemId = itemId,
                ItemType = normalizedType,
                CategoryId = categoryId,
                PersonalScore = savedRating?.PersonalScore ?? personalScore,
                Position = savedRating?.Position ?? 0,
                IsNewRating = isNewRating
            };
        }

        public async Task<RatingResponse> DeleteRatingAsync(string itemType, string itemId)
        {
            var userId = GetUserId();
            var normalizedType = itemType.ToLowerInvariant();
            var rating = await PrestigeDb.Ratings
                .FirstOrDefaultAsync(r => r.User.Id == userId && r.ItemId == itemId && r.ItemType.ToLower() == normalizedType);

            if (rating == null)
            {
                throw new NotFoundException(404, $"Rating not found for {normalizedType} {itemId}");
            }

            PrestigeDb.Ratings.Remove(rating);
            await PrestigeDb.SaveChangesAsync();

            // Recalculate remaining scores and positions for this type
            await RecalculateUserScoresAsync(userId, normalizedType);

            return new RatingResponse
            {
                ItemId = itemId,
                ItemType = normalizedType,
                IsNewRating = false,
                PersonalScore = null,
                Position = null,
                CategoryId = null
            };
        }

        private async Task RecalculateUserScoresAsync(string userId, string itemType)
        {
            // Batch load all necessary data in a single query
            var userRatings = await PrestigeDb.Ratings
                .Include(r => r.Category)
                .Where(r => r.User.Id == userId && r.ItemType == itemType)
                .ToListAsync();

            if (userRatings.Count == 0) return;

            // For tracks, recalculate scores within each album separately
            if (itemType == "track")
            {
                // Group by album and recalculate within each group
                var albumGroups = userRatings.GroupBy(r => r.AlbumId ?? "singles");
                
                foreach (var albumGroup in albumGroups)
                {
                    var albumRatings = albumGroup.OrderByDescending(r => r.PersonalScore).ToList();
                    RecalculateGroupScores(albumRatings);
                }
            }
            else
            {
                // For albums and artists, calculate globally
                var sortedRatings = userRatings.OrderByDescending(r => r.PersonalScore).ToList();
                RecalculateGroupScores(sortedRatings);
            }

            // Single save operation for all changes
            await PrestigeDb.SaveChangesAsync();
        }

        private async Task RecalculateUserScoresWithPositionAsync(string userId, string itemType, string newItemId, int insertPosition)
        {
            // Get all ratings for this user and item type
            var allRatings = await PrestigeDb.Ratings
                .Include(r => r.Category)
                .Where(r => r.User.Id == userId && r.ItemType == itemType)
                .ToListAsync();

            if (allRatings.Count == 0) return;

            // For tracks, handle album-based grouping
            if (itemType == "track")
            {
                var newItem = allRatings.FirstOrDefault(r => r.ItemId == newItemId);
                var albumId = newItem?.AlbumId;
                
                if (!string.IsNullOrEmpty(albumId))
                {
                    // Only recalculate within the same album
                    var albumRatings = allRatings.Where(r => r.AlbumId == albumId).ToList();
                    RecalculatePositionBasedScores(albumRatings, newItemId, insertPosition);
                }
                else
                {
                    // Singles or unknown album - recalculate all tracks
                    RecalculatePositionBasedScores(allRatings, newItemId, insertPosition);
                }
            }
            else
            {
                // For albums and artists, recalculate globally
                RecalculatePositionBasedScores(allRatings, newItemId, insertPosition);
            }

            await PrestigeDb.SaveChangesAsync();
        }

        private void RecalculatePositionBasedScores(List<Domain.Rating> ratings, string newItemId, int insertPosition)
        {
            if (ratings.Count == 0) return;

            // Group by category
            var categoryGroups = ratings.GroupBy(r => r.Category.Id).ToList();
            
            foreach (var categoryGroup in categoryGroups)
            {
                var categoryRatings = categoryGroup.ToList();
                var categoryBounds = categoryGroup.First().Category;
                var newItem = categoryRatings.FirstOrDefault(r => r.ItemId == newItemId);
                
                // Only process the category that contains the new item
                if (newItem == null) continue;

                // Remove new item temporarily
                var existingItems = categoryRatings.Where(r => r.ItemId != newItemId).ToList();
                
                // Sort existing items by current position
                existingItems.Sort((a, b) => a.Position.CompareTo(b.Position));
                
                // Insert new item at specified position
                var finalList = new List<Domain.Rating>();
                
                // Clamp insert position to valid range
                var insertIndex = Math.Max(0, Math.Min(insertPosition, existingItems.Count));
                
                Logger.LogInformation($"Inserting new item {newItemId} at position {insertIndex} out of {existingItems.Count} existing items");
                
                // Add items before insert position
                for (int i = 0; i < insertIndex; i++)
                {
                    finalList.Add(existingItems[i]);
                }
                
                // Add new item
                finalList.Add(newItem);
                
                // Add remaining items (they get pushed down by 1 position)
                for (int i = insertIndex; i < existingItems.Count; i++)
                {
                    finalList.Add(existingItems[i]);
                }
                
                // Recalculate scores - position 0 gets ceiling, others distributed evenly
                var minScore = categoryBounds.MinScore;
                var maxScore = categoryBounds.MaxScore;
                
                Logger.LogInformation($"Recalculating scores for {finalList.Count} items in category {categoryBounds.Name} (range: {minScore}-{maxScore})");
                
                for (int i = 0; i < finalList.Count; i++)
                {
                    var rating = finalList[i];
                    decimal newScore;
                    
                    if (finalList.Count == 1)
                    {
                        newScore = maxScore; // Single item gets ceiling
                    }
                    else
                    {
                        // Beli formula: position 0 gets maxScore, evenly distribute to minScore
                        // When new items are added, ALL scores redistribute
                        var normalizedPosition = (decimal)i / (finalList.Count - 1);
                        newScore = maxScore - (normalizedPosition * (maxScore - minScore));
                    }
                    
                    // Ensure score stays within bounds
                    newScore = Math.Max(minScore, Math.Min(maxScore, newScore));
                    
                    Logger.LogInformation($"Item {rating.ItemId} at position {i}: old score = {rating.PersonalScore:F2}, new score = {newScore:F2}");
                    
                    rating.UpdatePosition(i);
                    rating.UpdateScore(newScore);
                }
            }
        }

        private void RecalculateGroupScores(List<Domain.Rating> ratings)
        {
            if (ratings.Count == 0) return;

            // Group ratings by category to calculate scores within each partition
            var categoryGroups = ratings.GroupBy(r => r.Category.Id).ToList();
            
            foreach (var categoryGroup in categoryGroups)
            {
                var categoryRatings = categoryGroup.OrderBy(r => r.Position).ToList();
                var categoryBounds = categoryGroup.First().Category;
                
                // Calculate scores within category bounds
                // IMPORTANT: Top item ALWAYS gets the ceiling (maxScore)
                var minScore = categoryBounds.MinScore;
                var maxScore = categoryBounds.MaxScore;
                
                for (int i = 0; i < categoryRatings.Count; i++)
                {
                    var rating = categoryRatings[i];
                    
                    decimal newScore;
                    if (categoryRatings.Count == 1)
                    {
                        // Single item gets the ceiling
                        newScore = maxScore;
                    }
                    else
                    {
                        // Beli formula: Score = max - (position / (count - 1)) * (max - min)
                        // This ensures top item (position 0) gets maxScore
                        // and scores are evenly distributed down to minScore
                        var normalizedPosition = (decimal)i / (categoryRatings.Count - 1);
                        newScore = maxScore - (normalizedPosition * (maxScore - minScore));
                    }
                    
                    // Ensure score stays within bounds
                    newScore = Math.Max(minScore, Math.Min(maxScore, newScore));
                    
                    rating.UpdatePosition(i);
                    rating.UpdateScore(newScore);
                }
            }
        }

        private string GetUserId()
        {
            return Principal.FindFirst(ClaimTypes.NameIdentifier)?.Value?.Split("|").Last() 
                ?? throw new UnauthorizedAccessException("User ID not found");
        }

        private async Task<User> GetUserAsync(string userId)
        {
            return await PrestigeDb.Users.FirstOrDefaultAsync(u => u.Id == userId)
                ?? throw new NotFoundException(404, $"User {userId} not found");
        }
    }
}