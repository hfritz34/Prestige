using Microsoft.EntityFrameworkCore;
using Prestige.Api.Configuration;
using Prestige.Api.Data;
using Prestige.Api.Endpoints.Collection;
using Prestige.Api.Endpoints.Rating.RequestResponse;

namespace Prestige.Api.Endpoints.Rating;

public partial class RatingServices
{
    private bool UsesCatalogCollections => RuntimeConfiguration.IsCatalog(Config) && !RuntimeConfiguration.IsLocalDemo(Config);

    public Task<RatingResponse> SaveRatingAsync(string itemType, string itemId, int desiredPosition, int categoryId)
    {
        if (!UsesCatalogCollections) return SaveRatingCoreAsync(itemType, itemId, desiredPosition, categoryId);
        var type = CollectionServices.NormalizeType(itemType);
        if (desiredPosition < 0) throw new Exceptions.ArgumentException(400, "Rating position cannot be negative.");
        return CatalogMutationAsync(async () =>
        {
            await CollectionServices.EnsureSavedAsync(PrestigeDb, GetUserId(), type, itemId);
            return type == "track"
                ? await SaveRatingCoreAsync(type, itemId, desiredPosition, categoryId)
                : await SaveCatalogScoreAsync(type, itemId, desiredPosition, categoryId);
        });
    }

    public Task<RatingResponse> DeleteRatingAsync(string itemType, string itemId) => UsesCatalogCollections
        ? CatalogMutationAsync(() => DeleteRatingCoreAsync(CollectionServices.NormalizeType(itemType), itemId))
        : DeleteRatingCoreAsync(itemType, itemId);

    public Task<ComparisonResultResponse> SubmitComparisonAsync(ComparisonRequest request)
    {
        if (!UsesCatalogCollections) return SubmitComparisonCoreAsync(request);
        var type = CollectionServices.NormalizeType(request.ItemType);
        if (request.ItemId1 == request.ItemId2 || (request.WinnerId != request.ItemId1 && request.WinnerId != request.ItemId2))
            throw new Exceptions.ArgumentException(400, "Compare two different items and choose one of them as the winner.");
        return CatalogMutationAsync(async () =>
        {
            await CollectionServices.RequireKnownItemAsync(PrestigeDb, type, request.ItemId1);
            await CollectionServices.RequireKnownItemAsync(PrestigeDb, type, request.ItemId2);
            if (type == "track")
            {
                var albumIds = await PrestigeDb.Tracks
                    .Where(track => track.Id == request.ItemId1 || track.Id == request.ItemId2)
                    .Select(track => track.Album.Id).Distinct().ToListAsync();
                if (albumIds.Count != 1)
                    throw new Exceptions.ArgumentException(400, "Track comparisons must be within the same album.");
            }
            return await SubmitComparisonCoreAsync(new ComparisonRequest
            {
                ItemType = type, ItemId1 = request.ItemId1, ItemId2 = request.ItemId2, WinnerId = request.WinnerId
            });
        });
    }

    private async Task<RatingResponse> SaveCatalogScoreAsync(string type, string itemId, int position, int categoryId)
    {
        var userId = GetUserId();
        var category = await PrestigeDb.RatingCategories.FindAsync(categoryId)
            ?? throw new Exceptions.NotFoundException(404, "Rating category not found.");
        var ratings = await PrestigeDb.Ratings.Include(r => r.Category)
            .Where(r => r.User.Id == userId && r.ItemType == type).ToListAsync();
        var existing = ratings.SingleOrDefault(r => r.ItemId == itemId);
        var previousCategory = existing?.Category.Id;
        // Re-rating must not compare an item with itself or count its old score in its new position.
        var peers = ratings.Where(r => r.ItemId != itemId && r.Category.Id == categoryId)
            .OrderBy(r => r.Position).ThenBy(r => r.ItemId).ToList();
        var insertion = Math.Min(position, peers.Count);
        decimal score;
        if (peers.Count == 0) score = category.MaxScore;
        else if (insertion == 0)
        {
            score = category.MaxScore;
            await RedistributeScoresAfterNewTop(peers, category);
        }
        else if (insertion == peers.Count)
            score = Math.Max(peers[^1].PersonalScore - (peers[^1].PersonalScore - category.MinScore) / 2m,
                category.MinScore + 0.1m);
        else score = (peers[insertion - 1].PersonalScore + peers[insertion].PersonalScore) / 2m;

        var rating = existing ?? new Domain.Rating(await GetUserAsync(userId), itemId, type, category, insertion + 1, score);
        if (existing == null) PrestigeDb.Ratings.Add(rating);
        else rating.UpdateRating(score, category, insertion + 1);
        peers.Insert(insertion, rating);
        for (var index = 0; index < peers.Count; index++) peers[index].UpdatePosition(index + 1);
        if (previousCategory.HasValue && previousCategory != categoryId)
        {
            var previous = ratings.Where(r => r.ItemId != itemId && r.Category.Id == previousCategory)
                .OrderBy(r => r.Position).ThenBy(r => r.ItemId).ToList();
            for (var index = 0; index < previous.Count; index++) previous[index].UpdatePosition(index + 1);
        }
        return new RatingResponse { ItemId = itemId, ItemType = type, CategoryId = categoryId,
            PersonalScore = score, Position = insertion + 1, IsNewRating = existing == null };
    }

    private async Task<T> CatalogMutationAsync<T>(Func<Task<T>> mutation)
    {
        await using var transaction = await AccountWriteTransaction.BeginAsync(PrestigeDb, GetUserId());
        var result = await mutation();
        await PrestigeDb.SaveChangesAsync();
        await transaction.CommitAsync();
        return result;
    }
}
