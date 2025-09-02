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
                .AsNoTracking()  // Added for read-only query optimization
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
                .AsNoTracking()  // Added for read-only query optimization
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
                .ToListAsync();

            if (tracksInCategory.Count == 0) return;

            // Get all comparisons for tracks in this category and album
            var trackIds = tracksInCategory.Select(t => t.ItemId).ToList();
            var comparisons = await PrestigeDb.RatingComparisons
                .Where(c => c.User.Id == userId && 
                           c.ItemType.ToLower() == "track" &&
                           trackIds.Contains(c.ItemId1) && 
                           trackIds.Contains(c.ItemId2))
                .ToListAsync();

            // Order tracks based on comparison results using topological sort
            var orderedTracks = OrderTracksByComparisons(tracksInCategory, comparisons, targetTrackId, desiredPosition);

            // Update positions (1-based)
            for (int i = 0; i < orderedTracks.Count; i++)
            {
                orderedTracks[i].UpdatePosition(i + 1);
            }
        }

        private List<Domain.Rating> OrderTracksByComparisons(List<Domain.Rating> tracks, List<RatingComparison> comparisons, string targetTrackId, int desiredPosition)
        {
            Console.WriteLine($"DEBUG ORDER: Starting with {tracks.Count} tracks and {comparisons.Count} comparisons");
            
            // If there's only one track, just return it
            if (tracks.Count <= 1) return tracks;

            // Build a graph of wins/losses from comparisons, handling contradictory results
            var winGraph = BuildResolvedWinGraph(tracks, comparisons);

            // Find the target track and handle desired position
            var targetTrack = tracks.FirstOrDefault(t => t.ItemId == targetTrackId);
            if (targetTrack != null && desiredPosition > 0)
            {
                // For new comparisons, we'll place the target track at the desired position
                // and adjust other tracks accordingly
                return PlaceTrackAtDesiredPosition(tracks, targetTrack, desiredPosition, winGraph);
            }

            // If no target track or comparison-based ordering, use topological sort
            return TopologicalSort(tracks, winGraph);
        }

        private Dictionary<string, HashSet<string>> BuildResolvedWinGraph(List<Domain.Rating> tracks, List<RatingComparison> comparisons)
        {
            Console.WriteLine($"DEBUG WIN GRAPH: Building resolved win graph");
            
            var winGraph = new Dictionary<string, HashSet<string>>();
            foreach (var track in tracks)
            {
                winGraph[track.ItemId] = new HashSet<string>();
            }

            // Group comparisons by track pairs to handle contradictory results
            var pairComparisons = new Dictionary<string, List<RatingComparison>>();
            
            foreach (var comparison in comparisons)
            {
                var trackIds = new[] { comparison.ItemId1, comparison.ItemId2 }.OrderBy(x => x).ToArray();
                var pairKey = $"{trackIds[0]}|{trackIds[1]}";
                
                if (!pairComparisons.ContainsKey(pairKey))
                {
                    pairComparisons[pairKey] = new List<RatingComparison>();
                }
                pairComparisons[pairKey].Add(comparison);
            }

            // For each pair, determine the final winner
            foreach (var kvp in pairComparisons)
            {
                var pairKey = kvp.Key;
                var pairComparisonList = kvp.Value;
                var trackIds = pairKey.Split('|');
                var track1 = trackIds[0];
                var track2 = trackIds[1];

                Console.WriteLine($"DEBUG WIN GRAPH: Processing pair {track1} vs {track2} with {pairComparisonList.Count} comparisons");

                // Use the most recent comparison as the final result
                var mostRecentComparison = pairComparisonList.OrderByDescending(c => c.ComparisonDate).First();
                var winnerId = mostRecentComparison.WinnerId;
                var loserId = mostRecentComparison.ItemId1 == winnerId ? mostRecentComparison.ItemId2 : mostRecentComparison.ItemId1;

                Console.WriteLine($"DEBUG WIN GRAPH: Most recent comparison: {winnerId} beats {loserId} (from {mostRecentComparison.ComparisonDate})");

                // Add the win relationship
                if (winGraph.ContainsKey(winnerId) && winGraph.ContainsKey(loserId))
                {
                    winGraph[winnerId].Add(loserId);
                    Console.WriteLine($"DEBUG WIN GRAPH: Added {winnerId} beats {loserId}");
                }
            }

            return winGraph;
        }

        private List<Domain.Rating> PlaceTrackAtDesiredPosition(List<Domain.Rating> tracks, Domain.Rating targetTrack, int desiredPosition, Dictionary<string, HashSet<string>> winGraph)
        {
            // Get other tracks and sort them by current position
            var otherTracks = tracks.Where(t => t.ItemId != targetTrack.ItemId).OrderBy(t => t.Position).ToList();
            
            // Ensure desired position is valid
            var effectiveDesiredPosition = Math.Max(1, Math.Min(desiredPosition, tracks.Count));
            
            var result = new List<Domain.Rating>();
            
            // Add tracks before the desired position
            for (int i = 0; i < effectiveDesiredPosition - 1 && i < otherTracks.Count; i++)
            {
                result.Add(otherTracks[i]);
            }
            
            // Add the target track at desired position
            result.Add(targetTrack);
            
            // Add remaining tracks after the desired position
            for (int i = effectiveDesiredPosition - 1; i < otherTracks.Count; i++)
            {
                result.Add(otherTracks[i]);
            }
            
            return result;
        }

        private List<Domain.Rating> TopologicalSort(List<Domain.Rating> tracks, Dictionary<string, HashSet<string>> winGraph)
        {
            Console.WriteLine($"DEBUG TOPO: Starting topological sort with {tracks.Count} tracks");
            
            // Debug: Print win graph
            Console.WriteLine($"DEBUG TOPO: Win graph contents:");
            foreach (var kvp in winGraph)
            {
                Console.WriteLine($"DEBUG TOPO: Track {kvp.Key} beats: [{string.Join(", ", kvp.Value)}]");
            }
            
            // Calculate in-degrees (how many tracks beat this track)
            var inDegree = new Dictionary<string, int>();
            foreach (var track in tracks)
            {
                inDegree[track.ItemId] = 0;
            }

            foreach (var kvp in winGraph)
            {
                foreach (var beaten in kvp.Value)
                {
                    if (inDegree.ContainsKey(beaten))
                    {
                        inDegree[beaten]++;
                    }
                }
            }

            Console.WriteLine($"DEBUG TOPO: In-degrees:");
            foreach (var kvp in inDegree)
            {
                Console.WriteLine($"DEBUG TOPO: Track {kvp.Key} has in-degree {kvp.Value}");
            }

            // Kahn's algorithm for topological sorting
            var queue = new Queue<string>();
            var result = new List<Domain.Rating>();
            var trackDict = tracks.ToDictionary(t => t.ItemId);

            // Start with tracks that have no incoming edges (nobody beats them)
            foreach (var kvp in inDegree)
            {
                if (kvp.Value == 0)
                {
                    Console.WriteLine($"DEBUG TOPO: Adding track {kvp.Key} to queue (no incoming edges)");
                    queue.Enqueue(kvp.Key);
                }
            }

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                Console.WriteLine($"DEBUG TOPO: Processing track {current}");
                if (trackDict.ContainsKey(current))
                {
                    result.Add(trackDict[current]);
                }

                // Remove edges from current node
                if (winGraph.ContainsKey(current))
                {
                    foreach (var beaten in winGraph[current])
                    {
                        if (inDegree.ContainsKey(beaten))
                        {
                            inDegree[beaten]--;
                            Console.WriteLine($"DEBUG TOPO: Reduced in-degree of {beaten} to {inDegree[beaten]}");
                            if (inDegree[beaten] == 0)
                            {
                                Console.WriteLine($"DEBUG TOPO: Adding track {beaten} to queue (in-degree now 0)");
                                queue.Enqueue(beaten);
                            }
                        }
                    }
                }
            }

            Console.WriteLine($"DEBUG TOPO: Topological sort result: {result.Count} tracks sorted");
            
            // If we couldn't sort all tracks (circular dependencies), add remaining tracks by current position
            var remainingTracks = tracks.Where(t => !result.Any(r => r.ItemId == t.ItemId))
                                       .OrderBy(t => t.Position)
                                       .ToList();
            
            if (remainingTracks.Count > 0)
            {
                Console.WriteLine($"DEBUG TOPO: Adding {remainingTracks.Count} remaining tracks (potential circular dependencies)");
                foreach (var track in remainingTracks)
                {
                    Console.WriteLine($"DEBUG TOPO: Adding remaining track {track.ItemId} at position {track.Position}");
                }
            }
            
            result.AddRange(remainingTracks);

            return result;
        }

        private async Task RecalculateTrackPositionsAfterDeletion(string userId, string albumId, int categoryId)
        {
            Console.WriteLine($"DEBUG RECALC: Starting position recalculation for album {albumId}, category {categoryId}");
            
            // Get all remaining tracks in this category for this album (after deletion)
            var remainingTracks = await PrestigeDb.Ratings
                .Where(r => r.User.Id == userId && r.ItemType.ToLower() == "track" && r.Category.Id == categoryId)
                .Join(PrestigeDb.Tracks.Include(t => t.Album),
                      r => r.ItemId,
                      t => t.Id,
                      (r, t) => new { r, t })
                .Where(x => x.t.Album.Id == albumId)
                .Select(x => x.r)
                .ToListAsync();

            Console.WriteLine($"DEBUG RECALC: Found {remainingTracks.Count} remaining tracks in category {categoryId}");
            
            if (remainingTracks.Count == 0) return;

            // Get all comparisons for remaining tracks
            var trackIds = remainingTracks.Select(t => t.ItemId).ToList();
            var comparisons = await PrestigeDb.RatingComparisons
                .Where(c => c.User.Id == userId && 
                           c.ItemType.ToLower() == "track" &&
                           trackIds.Contains(c.ItemId1) && 
                           trackIds.Contains(c.ItemId2))
                .ToListAsync();

            Console.WriteLine($"DEBUG RECALC: Found {comparisons.Count} comparisons for remaining tracks");
            foreach (var comp in comparisons.OrderBy(c => c.ComparisonDate))
            {
                Console.WriteLine($"DEBUG RECALC: Comparison {comp.ComparisonDate}: {comp.ItemId1} vs {comp.ItemId2}, Winner: {comp.WinnerId}");
            }

            // Use topological sort to order tracks based on comparisons
            var orderedTracks = TopologicalSort(remainingTracks, BuildResolvedWinGraph(remainingTracks, comparisons));

            // Update positions (1-based)
            for (int i = 0; i < orderedTracks.Count; i++)
            {
                Console.WriteLine($"DEBUG RECALC: Setting track {orderedTracks[i].ItemId} to position {i + 1}");
                orderedTracks[i].UpdatePosition(i + 1);
            }
            
            Console.WriteLine($"DEBUG RECALC: Position recalculation completed");
        }

        private Dictionary<string, HashSet<string>> BuildWinGraph(List<Domain.Rating> tracks, List<RatingComparison> comparisons)
        {
            var winGraph = new Dictionary<string, HashSet<string>>();
            foreach (var track in tracks)
            {
                winGraph[track.ItemId] = new HashSet<string>();
            }

            // Process comparisons to build win relationships
            foreach (var comparison in comparisons)
            {
                var winnerId = comparison.WinnerId;
                var loserId = comparison.ItemId1 == winnerId ? comparison.ItemId2 : comparison.ItemId1;
                
                // Winner beats loser
                if (winGraph.ContainsKey(winnerId) && winGraph.ContainsKey(loserId))
                {
                    winGraph[winnerId].Add(loserId);
                }
            }

            return winGraph;
        }

        public async Task<RatingResponse> DeleteRatingAsync(string itemType, string itemId)
        {
            var userId = GetUserId();
            var normalizedType = itemType.ToLowerInvariant();
            var rating = await PrestigeDb.Ratings
                .Include(r => r.Category)
                .FirstOrDefaultAsync(r => r.User.Id == userId && r.ItemId == itemId && r.ItemType.ToLower() == normalizedType);

            if (rating == null)
            {
                throw new NotFoundException(404, $"Rating not found for {normalizedType} {itemId}");
            }

            var albumId = rating.AlbumId;
            var categoryId = rating.Category.Id;

            Console.WriteLine($"DEBUG DELETE: Deleting track {itemId} from album {albumId}, category {categoryId}, position {rating.Position}, albumRank {rating.RankWithinAlbum}");

            PrestigeDb.Ratings.Remove(rating);
            await PrestigeDb.SaveChangesAsync();

            if (normalizedType == "track")
            {
                // For tracks, recalculate positions within the category after deletion
                if (!string.IsNullOrEmpty(albumId))
                {
                    Console.WriteLine($"DEBUG DELETE: Recalculating positions for album {albumId}, category {categoryId}");
                    await RecalculateTrackPositionsAfterDeletion(userId, albumId, categoryId);
                    
                    Console.WriteLine($"DEBUG DELETE: Updating album-wide ranks for album {albumId}");
                    // Recompute album-wide rankings across all categories
                    await UpdateAlbumRanksForTracks(userId, albumId);
                    await PrestigeDb.SaveChangesAsync();
                    
                    Console.WriteLine($"DEBUG DELETE: Final save completed");
                }
            }
            else
            {
                // For albums and artists, use the existing score-based recalculation
                await RecalculateUserScoresAsync(userId, normalizedType);
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
            Console.WriteLine($"DEBUG ALBUM RANKS: Starting album ranking update for album {albumId}");
            
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

            Console.WriteLine($"DEBUG ALBUM RANKS: Found {albumTracks.Count} total tracks in album");
            
            if (albumTracks.Count == 0) 
            {
                Console.WriteLine($"DEBUG ALBUM RANKS: No tracks found, returning");
                return;
            }

            foreach (var track in albumTracks)
            {
                Console.WriteLine($"DEBUG ALBUM RANKS: Before sort - Track {track.ItemId}, Category {track.Category.Name} (order {track.Category.DisplayOrder}), Position {track.Position}, AlbumRank {track.RankWithinAlbum}");
            }

            // Sort by category priority (loved > liked > disliked), then by position within category
            var sortedTracks = albumTracks
                .OrderBy(r => r.Category.DisplayOrder) // Category 1 (Loved) comes first
                .ThenBy(r => r.Position) // Lower positions (1, 2, 3...) within category come first
                .ToList();

            Console.WriteLine($"DEBUG ALBUM RANKS: After sort:");
            for (int i = 0; i < sortedTracks.Count; i++)
            {
                Console.WriteLine($"DEBUG ALBUM RANKS: Position {i + 1} - Track {sortedTracks[i].ItemId}, Category {sortedTracks[i].Category.Name}");
            }

            // Assign RankWithinAlbum based on sorted order (1-based for UI display)
            for (int i = 0; i < sortedTracks.Count; i++)
            {
                Console.WriteLine($"DEBUG ALBUM RANKS: Setting track {sortedTracks[i].ItemId} album rank to {i + 1}");
                sortedTracks[i].UpdateRankWithinAlbum(i + 1);
            }
            
            Console.WriteLine($"DEBUG ALBUM RANKS: Album ranking update completed");
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