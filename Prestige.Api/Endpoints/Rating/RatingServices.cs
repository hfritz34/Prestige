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

            if (normalizedType == "track")
            {
                return await SaveTrackRatingAsync(userId, user, itemId, desiredPosition, category);
            }
            else
            {
                return await SaveAlbumArtistRatingAsync(userId, user, itemId, normalizedType, desiredPosition, category);
            }
        }

        private async Task<RatingResponse> SaveTrackRatingAsync(string userId, User user, string itemId, int desiredPosition, RatingCategory category)
        {
            // Get album ID for track
            var track = await PrestigeDb.Tracks
                .Include(t => t.Album)
                .FirstOrDefaultAsync(t => t.Id == itemId);
            var albumId = track?.Album?.Id;

            if (string.IsNullOrEmpty(albumId))
            {
                throw new NotFoundException(404, $"Album not found for track {itemId}");
            }

            // Check if rating already exists
            var existingRating = await PrestigeDb.Ratings
                .FirstOrDefaultAsync(r => r.User.Id == userId && r.ItemId == itemId && r.ItemType.ToLower() == "track");

            bool isNewRating = existingRating == null;

            // For tracks, we use simple position-based logic (no scores)
            // The PersonalScore will be set to the category's max score as a placeholder
            decimal placeholderScore = category.MaxScore;

            if (existingRating != null)
            {
                // Update existing rating - just change category
                existingRating.EnsureAlbumId(albumId);
                existingRating.UpdateRating(placeholderScore, category, 1); // Temporary position
            }
            else
            {
                // Create new rating
                var newRating = new Domain.Rating(user, itemId, "track", category, 1, placeholderScore, albumId);
                PrestigeDb.Ratings.Add(newRating);
            }

            await PrestigeDb.SaveChangesAsync();

            // Recalculate positions within the category for this album, considering desired position
            await RecalculateTrackPositionsInCategory(userId, albumId, category.Id, itemId, desiredPosition);

            // Update album-wide rankings across all categories
            await UpdateAlbumRanksForTracks(userId, albumId);
            await PrestigeDb.SaveChangesAsync();

            // Get the final position and rank
            var finalRating = await PrestigeDb.Ratings
                .FirstOrDefaultAsync(r => r.User.Id == userId && r.ItemId == itemId && r.ItemType.ToLower() == "track");

            return new RatingResponse
            {
                ItemId = itemId,
                ItemType = "track",
                CategoryId = category.Id,
                PersonalScore = placeholderScore,
                Position = finalRating?.Position ?? 1,
                RankWithinAlbum = finalRating?.RankWithinAlbum,
                IsNewRating = isNewRating
            };
        }

        private async Task<RatingResponse> SaveAlbumArtistRatingAsync(string userId, User user, string itemId, string itemType, int desiredPosition, RatingCategory category)
        {
            // Keep existing logic for albums and artists (score-based)
            var existingRating = await PrestigeDb.Ratings
                .FirstOrDefaultAsync(r => r.User.Id == userId && r.ItemId == itemId && r.ItemType.ToLower() == itemType);

            bool isNewRating = existingRating == null;

            // Get existing ratings in same category for score calculation
            var existingRatings = await PrestigeDb.Ratings
                .Where(r => r.User.Id == userId && r.ItemType.ToLower() == itemType && r.Category.Id == category.Id)
                .OrderByDescending(r => r.PersonalScore)
                .ToListAsync();

            // Calculate score based on position within category bounds
            decimal calculatedScore;
            if (existingRatings.Count == 0)
            {
                calculatedScore = category.MaxScore;
            }
            else if (desiredPosition == 0)
            {
                calculatedScore = category.MaxScore;
                await RedistributeScoresAfterNewTop(existingRatings, category);
            }
            else if (desiredPosition >= existingRatings.Count)
            {
                var lowestScore = existingRatings.Last().PersonalScore;
                var availableRange = lowestScore - category.MinScore;
                calculatedScore = Math.Max(
                    lowestScore - (availableRange / 2m),
                    category.MinScore + 0.1m
                );
            }
            else
            {
                var higherItem = existingRatings[desiredPosition - 1];
                var lowerItem = existingRatings[desiredPosition];
                calculatedScore = (higherItem.PersonalScore + lowerItem.PersonalScore) / 2m;
            }

            var finalPosition = await CalculatePositionFromScore(userId, itemType, category.Id, calculatedScore, null) + 1;

            if (existingRating != null)
            {
                existingRating.UpdateRating(calculatedScore, category, finalPosition);
                await UpdatePositionsAfterScoreChange(userId, itemType, category.Id, itemId, calculatedScore, null);
            }
            else
            {
                var newRating = new Domain.Rating(user, itemId, itemType, category, finalPosition, calculatedScore);
                PrestigeDb.Ratings.Add(newRating);
                await UpdatePositionsAfterInsert(userId, itemType, category.Id, calculatedScore, null);
            }

            await PrestigeDb.SaveChangesAsync();

            return new RatingResponse
            {
                ItemId = itemId,
                ItemType = itemType,
                CategoryId = category.Id,
                PersonalScore = calculatedScore,
                Position = finalPosition,
                RankWithinAlbum = null,
                IsNewRating = isNewRating
            };
        }

        private async Task RecalculateTrackPositionsInCategory(string userId, string albumId, int categoryId, string targetTrackId, int desiredPosition)
        {
            // Get all tracks in this category for this album
            var tracksInCategory = await PrestigeDb.Ratings
                .Where(r => r.User.Id == userId && r.ItemType.ToLower() == "track" && r.Category.Id == categoryId)
                .Join(PrestigeDb.Tracks.Include(t => t.Album),
                      r => r.ItemId,
                      t => t.Id,
                      (r, t) => new { r, t })
                .Where(x => x.t.Album.Id == albumId)
                .Select(x => x.r)
                .OrderBy(x => x.Position)
                .ToListAsync();

            if (tracksInCategory.Count == 0) return;

            // Find the target track
            var targetTrack = tracksInCategory.FirstOrDefault(t => t.ItemId == targetTrackId);
            if (targetTrack == null) return;

            // Remove target track from list to reorder others
            var otherTracks = tracksInCategory.Where(t => t.ItemId != targetTrackId).ToList();

            // Handle desired position (convert 0-based to 1-based if needed)
            var effectiveDesiredPosition = desiredPosition <= 0 ? 1 : Math.Min(desiredPosition, tracksInCategory.Count);

            // Create new ordered list
            var reorderedTracks = new List<Domain.Rating>();
            
            // Add tracks before the desired position
            for (int i = 0; i < effectiveDesiredPosition - 1 && i < otherTracks.Count; i++)
            {
                reorderedTracks.Add(otherTracks[i]);
            }

            // Add the target track at desired position
            reorderedTracks.Add(targetTrack);

            // Add remaining tracks after the desired position
            for (int i = effectiveDesiredPosition - 1; i < otherTracks.Count; i++)
            {
                reorderedTracks.Add(otherTracks[i]);
            }

            // Update positions (1-based)
            for (int i = 0; i < reorderedTracks.Count; i++)
            {
                reorderedTracks[i].UpdatePosition(i + 1);
            }
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

            Console.WriteLine($"DEBUG UpdateAlbumRanks: Found {albumTracks.Count} rated tracks for album {albumId}");
            foreach (var track in albumTracks)
            {
                Console.WriteLine($"DEBUG: Before sort - Track {track.ItemId}, Score: {track.PersonalScore}, Category: {track.Category.Name} (DisplayOrder: {track.Category.DisplayOrder})");
            }

            // Sort by category priority (loved > liked > disliked), then by score within category
            var sortedTracks = albumTracks
                .OrderBy(r => r.Category.DisplayOrder) // Category 1 (Loved) comes first
                .ThenByDescending(r => r.PersonalScore) // Higher scores within category come first
                .ToList();

            Console.WriteLine($"DEBUG: After sort:");
            for (int i = 0; i < sortedTracks.Count; i++)
            {
                Console.WriteLine($"DEBUG: Position {i + 1} - Track {sortedTracks[i].ItemId}, Score: {sortedTracks[i].PersonalScore}, Category: {sortedTracks[i].Category.Name}");
            }

            // Assign RankWithinAlbum based on sorted order (1-based for UI display)
            for (int i = 0; i < sortedTracks.Count; i++)
            {
                sortedTracks[i].UpdateRankWithinAlbum(i + 1);
                Console.WriteLine($"DEBUG: Assigned rank {i + 1} to track {sortedTracks[i].ItemId}");
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