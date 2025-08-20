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
                    RankWithinAlbum = existingRating.RankWithinAlbum,
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
                    RankWithinAlbum = r.RankWithinAlbum,
                    AlbumId = r.AlbumId, // Now directly from the Rating entity
                    IsNewRating = false
                })
                .ToListAsync();

            return ratings;
        }

        public async Task<RatingResponse> SaveRatingAsync(string itemType, string itemId, int desiredPosition, int categoryId)
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

            // Get existing ratings in same category/album for score calculation
            IQueryable<Domain.Rating> existingQuery = PrestigeDb.Ratings
                .Where(r => r.User.Id == userId && r.ItemType.ToLower() == normalizedType && r.Category.Id == categoryId);

            if (normalizedType == "track" && !string.IsNullOrEmpty(albumId))
            {
                // Filter by album using track join to handle legacy/null AlbumId values on Rating
                existingQuery = existingQuery
                    .Join(PrestigeDb.Tracks.Include(t => t.Album),
                          r => r.ItemId,
                          t => t.Id,
                          (r, t) => new { r, t })
                    .Where(x => x.t.Album.Id == albumId)
                    .Select(x => x.r);
            }

            var existingRatings = await existingQuery
                .OrderByDescending(r => r.PersonalScore)
                .ToListAsync();

            // Calculate score based on position within category bounds (binary search result)
            decimal calculatedScore;
            if (existingRatings.Count == 0)
            {
                // First item gets max score
                calculatedScore = category.MaxScore;
            }
            else if (desiredPosition == 0)
            {
                // New top item - give it max score, push others down
                calculatedScore = category.MaxScore;
                // Redistribute existing items' scores downward
                await RedistributeScoresAfterNewTop(existingRatings, category);
            }
            else if (desiredPosition >= existingRatings.Count)
            {
                // New bottom item
                var lowestScore = existingRatings.Last().PersonalScore;
                var availableRange = lowestScore - category.MinScore;
                calculatedScore = Math.Max(
                    lowestScore - (availableRange / 2m),
                    category.MinScore + 0.1m
                );
            }
            else
            {
                // Insert between existing items
                var higherItem = existingRatings[desiredPosition - 1];
                var lowerItem = existingRatings[desiredPosition];
                calculatedScore = (higherItem.PersonalScore + lowerItem.PersonalScore) / 2m;
            }

            // Calculate final position based on the new score (convert to 1-based)
            var finalPosition = await CalculatePositionFromScore(userId, normalizedType, categoryId, calculatedScore, albumId) + 1;

            if (existingRating != null)
            {
                // Update existing rating
                existingRating.EnsureAlbumId(albumId);
                existingRating.UpdateRating(calculatedScore, category, finalPosition);
                // Update positions of other items in the same category
                await UpdatePositionsAfterScoreChange(userId, normalizedType, categoryId, itemId, calculatedScore, albumId);
            }
            else
            {
                // Create new rating
                var newRating = new Domain.Rating(user, itemId, normalizedType, category, finalPosition, calculatedScore, albumId);
                PrestigeDb.Ratings.Add(newRating);
                // Update positions of other items that are now ranked lower
                await UpdatePositionsAfterInsert(userId, normalizedType, categoryId, calculatedScore, albumId);
            }

            await PrestigeDb.SaveChangesAsync();

            // Get the updated RankWithinAlbum if this is a track
            int? rankWithinAlbum = null;
            if (normalizedType == "track" && !string.IsNullOrEmpty(albumId))
            {
                var updatedRating = await PrestigeDb.Ratings
                    .FirstOrDefaultAsync(r => r.User.Id == userId && r.ItemId == itemId && r.ItemType.ToLower() == normalizedType);
                rankWithinAlbum = updatedRating?.RankWithinAlbum;
            }

            return new RatingResponse
            {
                ItemId = itemId,
                ItemType = normalizedType,
                CategoryId = categoryId,
                PersonalScore = calculatedScore,
                Position = finalPosition,
                RankWithinAlbum = rankWithinAlbum,
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

            var albumId = rating.AlbumId;

            PrestigeDb.Ratings.Remove(rating);
            await PrestigeDb.SaveChangesAsync();

            // Recalculate remaining scores and positions for this type
            await RecalculateUserScoresAsync(userId, normalizedType);

            // For tracks, recompute album rank within album after deletion
            if (normalizedType == "track" && !string.IsNullOrEmpty(albumId))
            {
                await UpdateAlbumRanksForTracks(userId, albumId);
            }

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
            // Get all ratings for this user and item type with categories
            var userRatings = await PrestigeDb.Ratings
                .Include(r => r.Category)
                .Where(r => r.User.Id == userId && r.ItemType.ToLower() == itemType)
                .ToListAsync();

            if (userRatings.Count == 0) return;

            if (itemType == "track")
            {
                // For tracks, redistribute within each album/category combination
                var albumGroups = userRatings.GroupBy(r => r.AlbumId ?? "singles");
                foreach (var albumGroup in albumGroups)
                {
                    var categoryGroups = albumGroup.GroupBy(r => r.Category.Id);
                    foreach (var categoryGroup in categoryGroups)
                    {
                        await RedistributeScoresInCategory(categoryGroup.ToList(), categoryGroup.First().Category);
                    }
                }
            }
            else
            {
                // For albums/artists, redistribute within each category
                var categoryGroups = userRatings.GroupBy(r => r.Category.Id);
                foreach (var categoryGroup in categoryGroups)
                {
                    await RedistributeScoresInCategory(categoryGroup.ToList(), categoryGroup.First().Category);
                }
            }

            await PrestigeDb.SaveChangesAsync();
        }

        private async Task RedistributeScoresInCategory(List<Domain.Rating> ratings, Domain.RatingCategory category)
        {
            if (ratings.Count == 0) return;

            // Sort by current score (highest first) to maintain relative order
            var sorted = ratings.OrderByDescending(r => r.PersonalScore).ToList();
            
            // Calculate new scores evenly distributed within category bounds
            var scoreRange = category.MaxScore - category.MinScore;
            var scoreStep = sorted.Count > 1 ? scoreRange / (sorted.Count - 1) : 0;

            for (int i = 0; i < sorted.Count; i++)
            {
                decimal newScore;
                if (sorted.Count == 1)
                {
                    // Single item gets max score for category
                    newScore = category.MaxScore;
                }
                else if (i == 0)
                {
                    // Top item gets max score
                    newScore = category.MaxScore;
                }
                else if (i == sorted.Count - 1)
                {
                    // Bottom item gets min score (+ small buffer)
                    newScore = category.MinScore + 0.1m;
                }
                else
                {
                    // Items in between are evenly distributed
                    newScore = category.MaxScore - (scoreStep * i);
                    newScore = Math.Max(newScore, category.MinScore + 0.1m);
                }

                sorted[i].UpdateRating(newScore, category, i + 1);
            }
        }
        
        private async Task RedistributeScoresAfterNewTop(List<Domain.Rating> existingRatings, Domain.RatingCategory category)
        {
            // When a new item becomes #1, push all existing items down slightly
            // Redistribute scores evenly within the remaining range
            if (existingRatings.Count == 0) return;

            var availableRange = category.MaxScore - category.MinScore;
            var scoreStep = availableRange / (existingRatings.Count + 1);

            for (int i = 0; i < existingRatings.Count; i++)
            {
                var newScore = category.MaxScore - (scoreStep * (i + 1));
                var adjustedScore = Math.Max(newScore, category.MinScore + 0.1m);
                existingRatings[i].UpdateRating(adjustedScore, existingRatings[i].Category, i + 2);
            }
        }

        private async Task<int> CalculatePositionFromScore(string userId, string itemType, int categoryId, decimal newScore, string? albumId)
        {
            var query = PrestigeDb.Ratings
                .Where(r => r.User.Id == userId && r.ItemType.ToLower() == itemType && r.Category.Id == categoryId);

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
            IQueryable<Domain.Rating> query = PrestigeDb.Ratings
                .Where(r => r.User.Id == userId && r.ItemType.ToLower() == itemType && r.Category.Id == categoryId);

            if (itemType == "track" && !string.IsNullOrEmpty(albumId))
            {
                query = query
                    .Join(PrestigeDb.Tracks.Include(t => t.Album),
                          r => r.ItemId,
                          t => t.Id,
                          (r, t) => new { r, t })
                    .Where(x => x.t.Album.Id == albumId)
                    .Select(x => x.r);
            }

            var ratingsToUpdate = await query.ToListAsync();

            // Sort by score descending (highest score gets position 1 after reassignment)
            var sortedRatings = ratingsToUpdate.OrderByDescending(r => r.PersonalScore).ToList();

            // Reassign positions (1-based)
            for (int i = 0; i < sortedRatings.Count; i++)
            {
                sortedRatings[i].UpdatePosition(i + 1);
            }

            // For tracks, also update RankWithinAlbum across all categories
            if (itemType == "track" && !string.IsNullOrEmpty(albumId))
            {
                await UpdateAlbumRanksForTracks(userId, albumId);
            }
        }

        private async Task UpdatePositionsAfterInsert(string userId, string itemType, int categoryId, decimal newScore, string? albumId)
        {
            // Get ALL ratings in this category/album (including the newly inserted one)
            IQueryable<Domain.Rating> query = PrestigeDb.Ratings
                .Where(r => r.User.Id == userId && r.ItemType.ToLower() == itemType && r.Category.Id == categoryId);

            if (itemType == "track" && !string.IsNullOrEmpty(albumId))
            {
                query = query
                    .Join(PrestigeDb.Tracks.Include(t => t.Album),
                          r => r.ItemId,
                          t => t.Id,
                          (r, t) => new { r, t })
                    .Where(x => x.t.Album.Id == albumId)
                    .Select(x => x.r);
            }

            var allRatings = await query.ToListAsync();

            // Sort by score descending and reassign ALL positions to avoid conflicts
            var sortedRatings = allRatings.OrderByDescending(r => r.PersonalScore).ToList();

            // Reassign all positions (1-based)
            for (int i = 0; i < sortedRatings.Count; i++)
            {
                sortedRatings[i].UpdatePosition(i + 1);
            }

            // For tracks, also update RankWithinAlbum across all categories
            if (itemType == "track" && !string.IsNullOrEmpty(albumId))
            {
                await UpdateAlbumRanksForTracks(userId, albumId);
            }
        }


        private async Task UpdateAlbumRanksForTracks(string userId, string albumId)
        {
            // Get all track ratings for this album across all categories
            var albumTracks = await PrestigeDb.Ratings
                .Include(r => r.Category)
                .Where(r => r.User.Id == userId && r.ItemType.ToLower() == "track")
                .Join(PrestigeDb.Tracks.Include(t => t.Album),
                      r => r.ItemId,
                      t => t.Id,
                      (r, t) => new { r, t })
                .Where(x => x.t.Album.Id == albumId)
                .Select(x => x.r)
                .ToListAsync();

            if (albumTracks.Count == 0) return;

            // Sort by category priority (loved > liked > disliked), then by score within category
            var sortedTracks = albumTracks
                .OrderBy(r => r.Category.DisplayOrder) // Category 1 (Loved) comes first
                .ThenByDescending(r => r.PersonalScore) // Higher scores within category come first
                .ToList();

            // Assign RankWithinAlbum based on sorted order (1-based for UI display)
            for (int i = 0; i < sortedTracks.Count; i++)
            {
                sortedTracks[i].UpdateRankWithinAlbum(i + 1);
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