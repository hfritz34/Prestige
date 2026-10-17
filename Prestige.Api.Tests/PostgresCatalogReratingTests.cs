using Microsoft.EntityFrameworkCore;
using Prestige.Api.Domain;
using Microsoft.Extensions.Configuration;
using Prestige.Api.Endpoints.Catalog;

namespace Prestige.Api.Tests;

public sealed partial class PostgresDatabaseTests
{
    [PostgresFact]
    public async Task AlbumAndArtistReratingPreservesChosenOrderAndRepairsBothCategories()
    {
        var principal = await SeedRatingCatalogAsync();
        await using var db = CreateContext();
        var artist = await db.Artists.SingleAsync();
        db.Albums.AddRange(new Album("album-2", "Second", [], [artist]), new Album("album-3", "Third", [], [artist]));
        db.Artists.AddRange(new Artist("artist-2", "Second", []), new Artist("artist-3", "Third", []));
        await db.SaveChangesAsync();
        foreach (var type in new[] { "album", "artist" })
        {
            var service = Ratings(db, principal);
            for (var index = 1; index <= 3; index++) await service.SaveRatingAsync(type, $"{type}-{index}", index - 1, 1);
            await service.SaveRatingAsync(type, $"{type}-1", 2, 1);
            db.ChangeTracker.Clear();
            var ordered = await db.Ratings.Where(r => r.ItemType == type).OrderBy(r => r.Position).ToListAsync();
            Assert.Equal(new[] { $"{type}-2", $"{type}-3", $"{type}-1" }, ordered.Select(r => r.ItemId));
            Assert.Equal(new[] { 1, 2, 3 }, ordered.Select(r => r.Position));
            await service.SaveRatingAsync(type, $"{type}-2", 0, 2);
            db.ChangeTracker.Clear();
            var remaining = await db.Ratings.Where(r => r.ItemType == type && r.Category.Id == 1).OrderBy(r => r.Position).ToListAsync();
            Assert.Equal(new[] { 1, 2 }, remaining.Select(r => r.Position));
            var single = await service.SaveRatingAsync(type, $"{type}-2", 0, 2);
            Assert.Equal(6.7m, single.PersonalScore);
            Assert.Equal(1, single.Position);
        }
        var reader = new CatalogRatingsServices(db, principal, new ConfigurationBuilder().Build());
        var albums = await reader.ListAsync("album");
        Assert.Equal(3, albums.Count);
        Assert.All(albums, item => { Assert.NotNull(item.Album); Assert.NotNull(item.Rating.PersonalScore); });
        Assert.All(await reader.ListAsync("artist"), item => Assert.NotNull(item.Artist));
        await Ratings(db, principal).SaveRatingAsync("track", "song-1", 1, 1);
        var song = Assert.Single(await reader.ListAsync("track"));
        Assert.Null(song.Rating.PersonalScore);
        Assert.Equal("album-1", song.Track!.Album.Id);
        Assert.Empty(await db.UserAlbums.ToListAsync());
        Assert.Empty(await db.UserArtists.ToListAsync());
    }
}
