using Microsoft.EntityFrameworkCore;
using Prestige.Api.Domain;
using Prestige.Api.Endpoints.Catalog;
using Prestige.Api.Endpoints.Spotify.RequestResponse;

namespace Prestige.Api.Tests;

public sealed partial class PostgresDatabaseTests
{
    [PostgresFact]
    public async Task CatalogSearchIsCaseInsensitiveBoundedLiteralAndReturnsNestedMetadataOnPostgres()
    {
        await using var db = CreateContext();
        await db.Database.MigrateAsync();
        var artist = new Artist("artist-1", "MixedCase Artist", [new Image("https://example.com/artist.jpg", 100, 100)]);
        var album = new Album("album-1", "MixedCase Album", [new Image("https://example.com/album.jpg", 100, 100)], [artist]);
        for (var index = 0; index < 25; index++)
            db.Tracks.Add(new Track(new TrackResponse { Id = $"song-{index:00}", Name = "Same Name", DurationMs = 120000 }, album, [artist]));
        db.Artists.AddRange(new Artist("literal-1", @"100%_music\live", []), new Artist("literal-2", "100percentXmusiclive", []));
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        var service = new CatalogSearchServices(db);
        var result = await service.SearchAsync("  MIXEDcase  ");
        Assert.Equal(20, result.Tracks.Count);
        Assert.Equal(Enumerable.Range(0, 20).Select(index => $"song-{index:00}"), result.Tracks.Select(track => track.Id));
        Assert.Single(result.Albums);
        Assert.Single(result.Artists);
        Assert.Equal("https://example.com/album.jpg", Assert.Single(result.Tracks[0].Album.Images).Url);
        Assert.Equal("https://example.com/artist.jpg", Assert.Single(Assert.Single(result.Tracks[0].Album.Artists).Images).Url);
        Assert.Equal("https://example.com/artist.jpg", Assert.Single(Assert.Single(result.Tracks[0].Artists).Images).Url);
        Assert.Equal(25, (await service.SearchAsync("mixedcase album", "track", 50)).Tracks.Count);
        foreach (var term in new[] { "%", "_", @"\" })
        {
            var literal = await service.SearchAsync(term, "artist");
            Assert.Equal("literal-1", Assert.Single(literal.Artists).Id);
            Assert.Empty(literal.Tracks);
            Assert.Empty(literal.Albums);
        }
        Assert.Empty(await db.CollectionEntries.ToListAsync());
        Assert.Empty(await db.Users.ToListAsync());
    }
}
