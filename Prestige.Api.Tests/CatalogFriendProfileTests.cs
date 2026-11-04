using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.TestHost;
using Prestige.Api.Endpoints.Catalog;
using Prestige.Api.Endpoints.Collection;

namespace Prestige.Api.Tests;

public sealed partial class CatalogAccountTests
{
    [Fact]
    public async Task OnlyAcceptedFriendsCanReadShowcaseAndRatingsAndRemovalRevokesAccess()
    {
        await SeedCatalogAsync();
        var owner = Client("auth0|owner");
        var viewer = Client("auth0|viewer");
        var stranger = Client("auth0|stranger");
        var ownerId = (await Me(owner)).GetProperty("id").GetString()!;
        var viewerId = (await Me(viewer)).GetProperty("id").GetString()!;
        var root = $"/api/catalog/friends/{ownerId}";
        (await owner.PostAsJsonAsync("/api/rating/save", new { itemType = "track", itemId = "song-1", position = 1, categoryId = 1 })).EnsureSuccessStatusCode();
        (await owner.PatchAsJsonAsync("/api/collection/track/song-2", new { isFavorite = true })).EnsureSuccessStatusCode();
        (await owner.PatchAsJsonAsync("/api/collection/album/album-1", new { isPinned = true })).EnsureSuccessStatusCode();
        // This saved song is not in the showcase, even though it has a shareable rating.
        var paths = new[] { root, root + "/ratings/track", root + "/showcase" };
        foreach (var path in paths)
        {
            Assert.Equal(HttpStatusCode.NotFound, (await viewer.GetAsync(path)).StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized, (await app.GetTestClient().GetAsync(path)).StatusCode);
        }
        (await viewer.PostAsJsonAsync($"/api/catalog/friends/requests/{ownerId}", new { })).EnsureSuccessStatusCode();
        foreach (var path in paths) Assert.Equal(HttpStatusCode.NotFound, (await viewer.GetAsync(path)).StatusCode);
        (await owner.PostAsJsonAsync($"/api/catalog/friends/requests/{viewerId}/accept", new { })).EnsureSuccessStatusCode();
        Assert.Equal(ownerId, (await viewer.GetFromJsonAsync<CatalogFriendResponse>(root))!.Id);
        var showcase = (await viewer.GetFromJsonAsync<List<CollectionResponse>>(root + "/showcase"))!;
        Assert.Equal(2, showcase.Count);
        Assert.DoesNotContain(showcase, item => item.ItemId == "song-1");
        Assert.All(showcase, item => Assert.True(item.IsFavorite || item.IsPinned));
        Assert.Single((await viewer.GetFromJsonAsync<List<CollectionResponse>>(root + "/showcase?skip=1&take=1"))!);
        (await owner.DeleteAsync("/api/collection/track/song-1")).EnsureSuccessStatusCode();
        var rating = Assert.Single((await viewer.GetFromJsonAsync<List<CatalogRatingResponse>>(root + "/ratings/track"))!);
        Assert.Equal("song-1", rating.Track!.Id);
        Assert.Equal("album-1", rating.Rating.AlbumId);
        Assert.Null(rating.Rating.PersonalScore);
        Assert.Equal(1, rating.Rating.RankWithinAlbum);
        Assert.Empty((await viewer.GetFromJsonAsync<List<CatalogRatingResponse>>("/api/catalog/ratings/track"))!);
        foreach (var path in paths) Assert.Equal(HttpStatusCode.NotFound, (await stranger.GetAsync(path)).StatusCode);
        foreach (var path in new[] { "/showcase?take=101", "/ratings/track?skip=-1", "/ratings/podcast" })
            Assert.Equal(HttpStatusCode.BadRequest, (await viewer.GetAsync(root + path)).StatusCode);
        (await owner.DeleteAsync($"/api/catalog/friends/{viewerId}")).EnsureSuccessStatusCode();
        foreach (var path in paths) Assert.Equal(HttpStatusCode.NotFound, (await viewer.GetAsync(path)).StatusCode);
    }
}
