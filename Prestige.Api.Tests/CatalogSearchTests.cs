using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Prestige.Api.Data;
using Prestige.Api.Endpoints.Catalog;
using Prestige.Api.Endpoints.Collection;

namespace Prestige.Api.Tests;

public sealed partial class CatalogAccountTests
{
    [Fact]
    public async Task CatalogSearchAllowsNewAccountsToFindAndSaveMusicWithoutSpotifyOrHistory()
    {
        await SeedCatalogAsync();
        var client = Client("auth0|new-fan");
        var result = (await client.GetFromJsonAsync<CatalogSearchResponse>("/api/catalog/search?query=%20aRTiST%20"))!;
        Assert.Equal(2, result.Tracks.Count);
        Assert.Single(result.Albums);
        Assert.Single(result.Artists);
        Assert.Equal("song-1", result.Tracks[0].Id);
        Assert.Equal("album-1", result.Tracks[0].Album.Id);
        Assert.Equal("artist-1", Assert.Single(result.Tracks[0].Artists).Id);
        Assert.Equal("artist-1", Assert.Single(result.Tracks[0].Album.Artists).Id);

        using (var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PrestigeContext>();
            Assert.Empty(await db.CollectionEntries.ToListAsync());
            Assert.Empty(await db.UserTracks.ToListAsync());
            Assert.Empty(await db.UserAlbums.ToListAsync());
            Assert.Empty(await db.UserArtists.ToListAsync());
            Assert.Empty((await db.Users.SingleAsync()).AccessToken);
        }
        var saved = await client.PatchAsJsonAsync($"/api/collection/track/{result.Tracks[0].Id}", new { isFavorite = true });
        saved.EnsureSuccessStatusCode();
        Assert.True((await saved.Content.ReadFromJsonAsync<CollectionResponse>())!.IsFavorite);

        var limited = (await client.GetFromJsonAsync<CatalogSearchResponse>("/api/catalog/search?query=album&itemType=TRACK&take=1"))!;
        Assert.Single(limited.Tracks);
        Assert.Empty(limited.Albums);
        Assert.Empty(limited.Artists);
        var missing = (await client.GetFromJsonAsync<CatalogSearchResponse>("/api/catalog/search?query=not-present"))!;
        Assert.Empty(missing.Tracks);
        Assert.Empty(missing.Albums);
        Assert.Empty(missing.Artists);
    }

    [Fact]
    public async Task CatalogSearchRequiresAuthenticationAndRejectsUnboundedOrInvalidRequests()
    {
        Assert.Equal(HttpStatusCode.Unauthorized,
            (await app.GetTestClient().GetAsync("/api/catalog/search?query=music")).StatusCode);
        var client = Client("auth0|searcher");
        foreach (var query in new[] { "", "query=%20", $"query={new string('x', 121)}",
                     "query=music&take=0", "query=music&take=51", "query=music&itemType=podcast" })
            Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync($"/api/catalog/search?{query}")).StatusCode);
    }
}
