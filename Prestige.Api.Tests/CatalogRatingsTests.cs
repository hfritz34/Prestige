using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.TestHost;
using Prestige.Api.Endpoints.Catalog;

namespace Prestige.Api.Tests;

public sealed partial class CatalogAccountTests
{
    [Fact]
    public async Task CatalogRatingsArePrivatePaginatedAndSurviveCollectionRemoval()
    {
        await SeedCatalogAsync();
        var client = Client("auth0|rater");
        foreach (var id in new[] { "song-1", "song-2" })
            (await client.PostAsJsonAsync("/api/rating/save", new { itemType = "track", itemId = id, position = 1, categoryId = 1 })).EnsureSuccessStatusCode();
        (await client.DeleteAsync("/api/collection/track/song-1")).EnsureSuccessStatusCode();
        var ratings = (await client.GetFromJsonAsync<List<CatalogRatingResponse>>("/api/catalog/ratings/track"))!;
        Assert.Equal(2, ratings.Count);
        Assert.All(ratings, r => { Assert.Null(r.Rating.PersonalScore); Assert.NotNull(r.Track); Assert.Equal("album-1", r.Track!.Album.Id); });
        var page = (await client.GetFromJsonAsync<List<CatalogRatingResponse>>("/api/catalog/ratings/track?skip=1&take=1"))!;
        Assert.Equal(ratings[1].Rating.ItemId, Assert.Single(page).Rating.ItemId);
        Assert.Empty((await Client("auth0|other").GetFromJsonAsync<List<CatalogRatingResponse>>("/api/catalog/ratings/track"))!);
        Assert.Equal(HttpStatusCode.Unauthorized, (await app.GetTestClient().GetAsync("/api/catalog/ratings/track")).StatusCode);
        foreach (var path in new[] { "podcast", "track?take=101", "track?skip=-1" })
            Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync($"/api/catalog/ratings/{path}")).StatusCode);
    }
}
