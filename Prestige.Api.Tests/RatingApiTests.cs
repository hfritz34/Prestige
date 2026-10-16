using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Prestige.Api.Data;
using Prestige.Api.Endpoints.Collection;

namespace Prestige.Api.Tests;

public sealed partial class CatalogAccountTests
{
    [Fact]
    public async Task RatingKnownMusicAddsCollectionMembershipAndPreservesCuration()
    {
        await SeedCatalogAsync();
        var client = Client("auth0|listener");
        (await client.PutAsJsonAsync("/api/collection/track/song-1", new { isPinned = true })).EnsureSuccessStatusCode();
        foreach (var (type, id) in new[] { ("track", "song-1") })
            (await client.PostAsJsonAsync("/api/rating/save", new { itemType = type, itemId = id, position = 0, categoryId = 1 })).EnsureSuccessStatusCode();
        var collection = (await client.GetFromJsonAsync<List<CollectionResponse>>("/api/collection"))!;
        Assert.Single(collection);
        Assert.All(collection, entry => Assert.NotNull(entry.Rating));
        Assert.True(collection.Single(entry => entry.ItemType == "track").IsPinned);
        (await client.DeleteAsync("/api/rating/user/track/song-1")).EnsureSuccessStatusCode();
        Assert.True((await client.GetFromJsonAsync<CollectionResponse>("/api/collection/track/song-1"))!.IsPinned);
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PrestigeContext>();
        Assert.Empty(await db.UserTracks.ToListAsync());
        Assert.Empty(await db.UserAlbums.ToListAsync());
        Assert.Empty(await db.UserArtists.ToListAsync());
    }

    [Fact]
    public async Task InvalidRatingsAndComparisonsReturnClientErrorsWithoutWriting()
    {
        await SeedCatalogAsync();
        var client = Client("auth0|listener");
        foreach (var (type, id, position, category, status) in new[]
        {
            ("unknown", "song-1", 0, 1, HttpStatusCode.BadRequest),
            ("album", "missing", 0, 1, HttpStatusCode.NotFound),
            ("track", "song-1", -1, 1, HttpStatusCode.BadRequest),
            ("track", "song-1", 0, 999, HttpStatusCode.NotFound)
        })
            Assert.Equal(status, (await client.PostAsJsonAsync("/api/rating/save", new { itemType = type, itemId = id, position, categoryId = category })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/rating/compare",
            new { itemType = "track", itemId1 = "song-1", itemId2 = "song-2", winnerId = "not-a-contender" })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.PostAsJsonAsync("/api/rating/compare",
            new { itemType = "track", itemId1 = "song-1", itemId2 = "missing", winnerId = "song-1" })).StatusCode);
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PrestigeContext>();
        Assert.Empty(await db.Ratings.ToListAsync());
        Assert.Empty(await db.RatingComparisons.ToListAsync());
        Assert.Empty(await db.CollectionEntries.ToListAsync());
    }
}
