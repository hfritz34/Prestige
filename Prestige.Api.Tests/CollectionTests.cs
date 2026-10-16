using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Prestige.Api.Data;
using Prestige.Api.Domain;
using Prestige.Api.Endpoints.Collection;
using Prestige.Api.Endpoints.Spotify.RequestResponse;

namespace Prestige.Api.Tests;

public sealed partial class CatalogAccountTests
{
    [Fact]
    public async Task CollectionCanBeSavedPinnedAndRemovedWithoutListeningHistory()
    {
        await SeedCatalogAsync();
        var client = Client("auth0|collector");
        var id = (await Me(client)).GetProperty("id").GetString()!;
        var response = await client.PutAsJsonAsync("/api/collection/TRACK/song-1", new { isFavorite = true, isPinned = true });
        response.EnsureSuccessStatusCode();
        var entry = (await response.Content.ReadFromJsonAsync<CollectionResponse>())!;
        Assert.Equal("track", entry.ItemType);
        Assert.Equal("First song", entry.Name);
        Assert.Equal(["Artist"], entry.Artists);
        Assert.True(entry.IsFavorite);
        Assert.True(entry.IsPinned);
        Assert.Null(entry.Rating);
        Assert.Equal("album-1", entry.AlbumId);
        Assert.True(entry.UpdatedAt >= entry.SavedAt);

        response = await client.PutAsJsonAsync("/api/collection/track/song-1", new { isFavorite = true, isPinned = true });
        response.EnsureSuccessStatusCode();
        var repeated = (await response.Content.ReadFromJsonAsync<CollectionResponse>())!;
        Assert.Equal(entry.SavedAt, repeated.SavedAt);
        Assert.Equal(entry.UpdatedAt, repeated.UpdatedAt);

        using (var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PrestigeContext>();
            Assert.Single(await db.CollectionEntries.ToListAsync());
            Assert.Empty(await db.UserTracks.ToListAsync());
            Assert.Empty(await db.UserAlbums.ToListAsync());
            Assert.Empty(await db.UserArtists.ToListAsync());
            var rating = new Rating(await db.Users.SingleAsync(user => user.Id == id), "song-1", "track",
                (await db.RatingCategories.FindAsync(1))!, 1, 10m, "album-1");
            rating.UpdateRankWithinAlbum(1);
            db.Ratings.Add(rating);
            await db.SaveChangesAsync();
        }

        var rated = await client.GetFromJsonAsync<CollectionResponse>("/api/collection/track/song-1");
        Assert.Equal(1, rated!.Rating!.RankWithinAlbum);
        Assert.Null(rated.Rating.Score); // A category placeholder must not masquerade as a global song score.
        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync("/api/collection/track/song-1")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync("/api/collection/track/song-1")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/api/collection/track/song-1")).StatusCode);
        using var verification = app.Services.CreateScope();
        Assert.Single(await verification.ServiceProvider.GetRequiredService<PrestigeContext>().Ratings.ToListAsync());
    }

    [Fact]
    public async Task CollectionsUseAuthenticatedOwnerAndRejectUnknownMusic()
    {
        await SeedCatalogAsync();
        var alice = Client("auth0|alice");
        var aliceId = (await Me(alice)).GetProperty("id").GetString()!;
        (await alice.PutAsJsonAsync("/api/collection/album/album-1", new { isPinned = true })).EnsureSuccessStatusCode();
        var bob = Client("auth0|bob");
        Assert.Empty((await bob.GetFromJsonAsync<List<CollectionResponse>>("/api/collection"))!);
        Assert.Equal(HttpStatusCode.NotFound, (await bob.GetAsync("/api/collection/album/album-1")).StatusCode);
        (await bob.PutAsJsonAsync("/api/collection/album/album-1", new { userId = aliceId, isPinned = false })).EnsureSuccessStatusCode();
        Assert.True((await alice.GetFromJsonAsync<CollectionResponse>("/api/collection/album/album-1"))!.IsPinned);
        await bob.DeleteAsync("/api/collection/album/album-1");
        Assert.Single((await alice.GetFromJsonAsync<List<CollectionResponse>>("/api/collection"))!);

        Assert.Equal(HttpStatusCode.NotFound, (await bob.PutAsJsonAsync("/api/collection/track/unknown", new { })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await bob.PutAsJsonAsync("/api/collection/podcast/song-1", new { })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await bob.GetAsync("/api/collection?take=101")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await bob.GetAsync("/api/collection?skip=-1")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await app.GetTestClient().GetAsync("/api/collection")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await app.GetTestClient().PutAsJsonAsync("/api/collection/track/song-1", new { })).StatusCode);
    }

    [Fact]
    public async Task CollectionFiltersAndPaginationReturnOnlyMatchingItems()
    {
        await SeedCatalogAsync();
        var client = Client("auth0|collector");
        foreach (var route in new[] { "track/song-1", "track/song-2", "album/album-1", "artist/artist-1" })
            (await client.PutAsJsonAsync($"/api/collection/{route}", new { isFavorite = route.StartsWith("track"), isPinned = route.StartsWith("album") })).EnsureSuccessStatusCode();
        var tracks = (await client.GetFromJsonAsync<List<CollectionResponse>>("/api/collection?itemType=TRACK&favoritesOnly=true"))!;
        Assert.Equal(2, tracks.Count);
        Assert.All(tracks, entry => Assert.Equal("track", entry.ItemType));
        var pins = (await client.GetFromJsonAsync<List<CollectionResponse>>("/api/collection?pinnedOnly=true"))!;
        Assert.Equal("album-1", Assert.Single(pins).ItemId);
        var page = (await client.GetFromJsonAsync<List<CollectionResponse>>("/api/collection?take=1&skip=1"))!;
        Assert.Single(page);
        Assert.Empty((await client.GetFromJsonAsync<List<CollectionResponse>>("/api/collection?skip=4"))!);
    }

    private async Task SeedCatalogAsync()
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PrestigeContext>();
        var artist = new Artist("artist-1", "Artist", []);
        var album = new Album("album-1", "Album", [], [artist]);
        db.Tracks.AddRange(
            new Track(new TrackResponse { Id = "song-1", Name = "First song", DurationMs = 180000 }, album, [artist]),
            new Track(new TrackResponse { Id = "song-2", Name = "Second song", DurationMs = 180000 }, album, [artist]));
        await db.SaveChangesAsync();
    }
}
