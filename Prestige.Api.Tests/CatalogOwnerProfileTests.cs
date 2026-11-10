using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.TestHost;
using Prestige.Api.Endpoints.Collection;

namespace Prestige.Api.Tests;

public sealed partial class CatalogAccountTests
{
    [Fact]
    public async Task OwnerShowcaseMatchesFriendViewWithoutExposingPrivateSavedMusic()
    {
        await SeedCatalogAsync();
        var owner = Client("auth0|owner");
        var friend = Client("auth0|friend");
        var ownerId = (await Me(owner)).GetProperty("id").GetString()!;
        var friendId = (await Me(friend)).GetProperty("id").GetString()!;
        (await owner.PatchAsJsonAsync("/api/collection/track/song-1", new { isFavorite = true, isPinned = true })).EnsureSuccessStatusCode();
        (await owner.PatchAsJsonAsync("/api/collection/album/album-1", new { isPinned = true })).EnsureSuccessStatusCode();
        (await owner.PatchAsJsonAsync("/api/collection/artist/artist-1", new { isFavorite = true })).EnsureSuccessStatusCode();
        (await owner.PatchAsJsonAsync("/api/collection/track/song-2", new { })).EnsureSuccessStatusCode();
        (await owner.PostAsJsonAsync("/api/rating/save", new { itemType = "track", itemId = "song-1", position = 1, categoryId = 1 })).EnsureSuccessStatusCode();

        var showcase = (await owner.GetFromJsonAsync<List<CollectionResponse>>("/api/collection/showcase"))!;
        Assert.Equal(3, showcase.Count); // Favorite + pinned is a union, without duplicates.
        Assert.DoesNotContain(showcase, entry => entry.ItemId == "song-2");
        Assert.Equal(1, showcase.Single(entry => entry.ItemId == "song-1").Rating!.RankWithinAlbum);
        Assert.Equal(4, (await owner.GetFromJsonAsync<List<CollectionResponse>>("/api/collection"))!.Count);
        var page = (await owner.GetFromJsonAsync<List<CollectionResponse>>("/api/collection/showcase?skip=1&take=1"))!;
        Assert.Equal(showcase[1].ItemId, Assert.Single(page).ItemId);

        (await owner.PostAsJsonAsync($"/api/catalog/friends/requests/{friendId}", new { })).EnsureSuccessStatusCode();
        (await friend.PostAsJsonAsync($"/api/catalog/friends/requests/{ownerId}/accept", new { })).EnsureSuccessStatusCode();
        var shared = (await friend.GetFromJsonAsync<List<CollectionResponse>>($"/api/catalog/friends/{ownerId}/showcase"))!;
        Assert.Equal(showcase.Select(entry => entry.ItemId), shared.Select(entry => entry.ItemId));
        Assert.Empty((await friend.GetFromJsonAsync<List<CollectionResponse>>($"/api/collection/showcase?userId={ownerId}"))!);

        (await owner.PatchAsJsonAsync("/api/collection/track/song-1", new { isFavorite = false })).EnsureSuccessStatusCode();
        Assert.Contains((await owner.GetFromJsonAsync<List<CollectionResponse>>("/api/collection/showcase"))!, entry => entry.ItemId == "song-1" && entry.IsPinned);
        (await owner.PatchAsJsonAsync("/api/collection/track/song-1", new { isPinned = false })).EnsureSuccessStatusCode();
        Assert.DoesNotContain((await owner.GetFromJsonAsync<List<CollectionResponse>>("/api/collection/showcase"))!, entry => entry.ItemId == "song-1");
        Assert.NotNull((await owner.GetFromJsonAsync<CollectionResponse>("/api/collection/track/song-1"))!.Rating);
        foreach (var query in new[] { "take=101", "take=0", "skip=-1" })
            Assert.Equal(HttpStatusCode.BadRequest, (await owner.GetAsync($"/api/collection/showcase?{query}")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await app.GetTestClient().GetAsync("/api/collection/showcase")).StatusCode);
    }
}
