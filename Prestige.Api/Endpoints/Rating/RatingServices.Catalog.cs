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
            return await SaveRatingCoreAsync(type, itemId, desiredPosition, categoryId);
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

    private async Task<T> CatalogMutationAsync<T>(Func<Task<T>> mutation)
    {
        await using var transaction = await AccountWriteTransaction.BeginAsync(PrestigeDb, GetUserId());
        var result = await mutation();
        await PrestigeDb.SaveChangesAsync();
        await transaction.CommitAsync();
        return result;
    }
}
