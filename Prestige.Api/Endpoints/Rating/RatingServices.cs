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
                .OrderByDescending(r => r.PersonalScore)
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

            // CRITICAL FIX: personalScore IS the calculated score from frontend, NOT position
            var calculatedScore = personalScore;

            // Calculate position based on score (higher score = lower position number)
            var position = await CalculatePositionFromScore(userId, normalizedType, categoryId, calculatedScore, albumId);

            if (existingRating != null)
            {
                // Update existing rating
                existingRating.UpdateRating(calculatedScore, category, position);
                // Update positions of other items in the same category
                await UpdatePositionsAfterScoreChange(userId, normalizedType, categoryId, itemId, calculatedScore, albumId);
            }
            else
            {
                // Create new rating
                var newRating = new Domain.Rating(user, itemId, normalizedType, category, position, calculatedScore, albumId);
                PrestigeDb.Ratings.Add(newRating);
                // Update positions of other items that are now ranked lower
                await UpdatePositionsAfterInsert(userId, normalizedType, categoryId, calculatedScore, albumId);
            }

            await PrestigeDb.SaveChangesAsync();

            return new RatingResponse
            {
                ItemId = itemId,
                ItemType = normalizedType,
                CategoryId = categoryId,
                PersonalScore = calculatedScore,
                Position = position,
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
            // Recalculate positions only, based on existing scores
            var userRatings = await PrestigeDb.Ratings
                .Include(r => r.Category)
                .Where(r => r.User.Id == userId && r.ItemType == itemType)
                .ToListAsync();

            if (userRatings.Count == 0) return;

            if (itemType == "track")
            {
                var albumGroups = userRatings.GroupBy(r => r.AlbumId ?? "singles");
                foreach (var albumGroup in albumGroups)
                {
                    var sorted = albumGroup
                        .OrderByDescending(r => r.PersonalScore)
                        .ToList();
                    for (int i = 0; i < sorted.Count; i++)
                    {
                        sorted[i].UpdatePosition(i);
                    }
                }
            }
            else
            {
                var sorted = userRatings
                    .OrderByDescending(r => r.PersonalScore)
                    .ToList();
                for (int i = 0; i < sorted.Count; i++)
                {
                    sorted[i].UpdatePosition(i);
                }
            }

            await PrestigeDb.SaveChangesAsync();
        }
        
        private async Task<int> CalculatePositionFromScore(string userId, string itemType, int categoryId, decimal newScore, string? albumId)
        {
            var query = PrestigeDb.Ratings
                .Where(r => r.User.Id == userId && r.ItemType == itemType && r.Category.Id == categoryId);

            // For tracks, only compare within same album if albumId is provided
            if (itemType == "track" && !string.IsNullOrEmpty(albumId))
            {
                query = query.Where(r => r.AlbumId == albumId);
            }

            // Count how many items have higher scores (they get lower position numbers)
            var higherScoreCount = await query.CountAsync(r => r.PersonalScore > newScore);

            return higherScoreCount; // Position = number of items with higher scores
        }

        private async Task UpdatePositionsAfterScoreChange(string userId, string itemType, int categoryId, string itemId, decimal newScore, string? albumId)
        {
            var query = PrestigeDb.Ratings
                .Where(r => r.User.Id == userId && r.ItemType == itemType && r.Category.Id == categoryId && r.ItemId != itemId);

            if (itemType == "track" && !string.IsNullOrEmpty(albumId))
            {
                query = query.Where(r => r.AlbumId == albumId);
            }

            var ratingsToUpdate = await query.ToListAsync();

            // Sort by score descending (highest score gets position 0)
            var sortedRatings = ratingsToUpdate.OrderByDescending(r => r.PersonalScore).ToList();

            // Reassign positions
            for (int i = 0; i < sortedRatings.Count; i++)
            {
                sortedRatings[i].UpdatePosition(i);
            }
        }

        private async Task UpdatePositionsAfterInsert(string userId, string itemType, int categoryId, decimal newScore, string? albumId)
        {
            var query = PrestigeDb.Ratings
                .Where(r => r.User.Id == userId && r.ItemType == itemType && r.Category.Id == categoryId);

            if (itemType == "track" && !string.IsNullOrEmpty(albumId))
            {
                query = query.Where(r => r.AlbumId == albumId);
            }

            var ratingsToUpdate = await query.ToListAsync();

            // Sort by score descending
            var sortedRatings = ratingsToUpdate.OrderByDescending(r => r.PersonalScore).ToList();

            // Reassign all positions
            for (int i = 0; i < sortedRatings.Count; i++)
            {
                sortedRatings[i].UpdatePosition(i);
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