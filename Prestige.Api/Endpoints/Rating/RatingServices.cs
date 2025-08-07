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
            var user = await GetUserAsync(userId);

            // Check if item already rated
            var existingRating = await PrestigeDb.Ratings
                .Include(r => r.Category)
                .FirstOrDefaultAsync(r => r.User.Id == userId && r.ItemId == itemId && r.ItemType == itemType);

            if (existingRating != null)
            {
                return new RatingResponse
                {
                    ItemId = itemId,
                    ItemType = itemType,
                    CategoryId = existingRating.Category.Id,
                    PersonalScore = existingRating.PersonalScore,
                    Position = existingRating.Position,
                    IsNewRating = false
                };
            }

            return new RatingResponse
            {
                ItemId = itemId,
                ItemType = itemType,
                IsNewRating = true
            };
        }

        public async Task<ComparisonResultResponse> SubmitComparisonAsync(ComparisonRequest request)
        {
            var userId = GetUserId();
            var user = await GetUserAsync(userId);

            // Record the comparison
            var comparison = new RatingComparison(user, request.ItemId1, request.ItemId2, request.ItemType, request.WinnerId);
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
            
            var ratings = await PrestigeDb.Ratings
                .Include(r => r.Category)
                .Where(r => r.User.Id == userId && r.ItemType == itemType)
                .Select(r => new RatingResponse
                {
                    ItemId = r.ItemId,
                    ItemType = r.ItemType,
                    CategoryId = r.Category.Id,
                    PersonalScore = r.PersonalScore,
                    Position = r.Position,
                    // When rating tracks, AlbumId helps client filter comparisons to the same album
                    AlbumId = r.ItemType == "track" ?
                        PrestigeDb.Tracks.Where(t => t.Id == r.ItemId).Select(t => t.Album.Id).FirstOrDefault() : null,
                    IsNewRating = false
                })
                .ToListAsync();

            return ratings;
        }

        public async Task<RatingResponse> SaveRatingAsync(string itemType, string itemId, decimal personalScore, int categoryId)
        {
            var userId = GetUserId();
            var user = await GetUserAsync(userId);
            var category = await PrestigeDb.RatingCategories.FindAsync(categoryId)
                ?? throw new NotFoundException(404, $"Category {categoryId} not found");

            // Check if rating already exists
            var existingRating = await PrestigeDb.Ratings
                .FirstOrDefaultAsync(r => r.User.Id == userId && r.ItemId == itemId && r.ItemType == itemType);

            bool isNewRating = existingRating == null;

            if (existingRating != null)
            {
                // Update existing rating
                existingRating.UpdateRating(personalScore, category, 0); // Position will be calculated later
            }
            else
            {
                // Create new rating
                var newRating = new Domain.Rating(user, itemId, itemType, category, 0, personalScore);
                PrestigeDb.Ratings.Add(newRating);
            }

            await PrestigeDb.SaveChangesAsync();

            // Recalculate all scores for this user and item type using Beli-style position-based scoring
            await RecalculateUserScoresAsync(userId, itemType);

            // Get the final position and score for the saved item
            var savedRating = await PrestigeDb.Ratings
                .FirstOrDefaultAsync(r => r.User.Id == userId && r.ItemId == itemId && r.ItemType == itemType);

            return new RatingResponse
            {
                ItemId = itemId,
                ItemType = itemType,
                CategoryId = categoryId,
                PersonalScore = savedRating?.PersonalScore ?? personalScore,
                Position = savedRating?.Position ?? 0,
                IsNewRating = isNewRating
            };
        }

        private async Task RecalculateUserScoresAsync(string userId, string itemType)
        {
            // Get all ratings for this user and item type, ordered by personal score (highest first)
            var userRatings = await PrestigeDb.Ratings
                .Where(r => r.User.Id == userId && r.ItemType == itemType)
                .OrderByDescending(r => r.PersonalScore)
                .ToListAsync();

            if (userRatings.Count == 0) return;

            // Implement Beli-style position-based scoring
            // Score = 10 * (totalCount - 1 - position) / (totalCount - 1)
            // This ensures top item gets 10.0, bottom gets 0.0, evenly distributed
            
            for (int i = 0; i < userRatings.Count; i++)
            {
                var rating = userRatings[i];
                var position = i; // 0-based position (0 = highest rated)
                var totalCount = userRatings.Count;
                
                // Calculate new score based on position
                decimal newScore;
                if (totalCount == 1)
                {
                    newScore = 10.0m; // Single item gets perfect score
                }
                else
                {
                    // Position-based scoring: higher position = higher score
                    newScore = 10.0m * (totalCount - 1 - position) / (totalCount - 1);
                }
                
                // Update both position and recalculated score
                rating.UpdatePosition(position);
                rating.UpdateScore(newScore);
            }

            await PrestigeDb.SaveChangesAsync();
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