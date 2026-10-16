using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Prestige.Api.Auth;
using Prestige.Api.Data;
using Prestige.Api.Domain;
using Prestige.Api.Endpoints.Rating;
using Prestige.Api.Endpoints.Spotify.RequestResponse;

namespace Prestige.Api.Tests;

public sealed partial class PostgresDatabaseTests
{
    [PostgresFact]
    public async Task RatingAndCollectionChangesRollBackWhenAnIntermediateSaveFails()
    {
        var principal = await SeedRatingCatalogAsync();
        await using (var failing = CreateContext(new FailSecondSave()))
            await Assert.ThrowsAsync<InvalidOperationException>(() => Ratings(failing, principal).SaveRatingAsync("track", "song-1", 0, 1));

        await using var db = CreateContext();
        Assert.Empty(await db.Ratings.ToListAsync());
        Assert.Empty(await db.CollectionEntries.ToListAsync());

        await Ratings(db, principal).SaveRatingAsync("track", "song-1", 0, 1);
        await using (var failing = CreateContext(new FailSecondSave()))
            await Assert.ThrowsAsync<InvalidOperationException>(() => Ratings(failing, principal).DeleteRatingAsync("track", "song-1"));

        db.ChangeTracker.Clear();
        var saved = await db.Ratings.SingleAsync();
        Assert.Equal(1, saved.RankWithinAlbum);
        Assert.Single(await db.CollectionEntries.ToListAsync());
    }

    [PostgresFact]
    public async Task ConcurrentRatingsKeepUniqueMembershipAndContiguousAlbumRanks()
    {
        var principal = await SeedRatingCatalogAsync();
        await Task.WhenAll(Enumerable.Range(0, 8).Select(async index =>
        {
            await using var db = CreateContext();
            await Ratings(db, principal).SaveRatingAsync("track", $"song-{index % 2 + 1}", 0, 1);
        }));
        await using var verification = CreateContext();
        var ratings = await verification.Ratings.OrderBy(rating => rating.RankWithinAlbum).ToListAsync();
        Assert.Equal(2, ratings.Count);
        Assert.Equal(new int?[] { 1, 2 }, ratings.Select(rating => rating.RankWithinAlbum));
        Assert.Equal(new[] { 1, 2 }, ratings.Select(rating => rating.Position));
        Assert.Equal(2, await verification.CollectionEntries.CountAsync());
        Assert.Empty(await verification.UserTracks.ToListAsync());
    }

    [PostgresFact]
    public async Task TrackCategoryChangesAndDeletionKeepPositionsWithinTheirAlbum()
    {
        var principal = await SeedRatingCatalogAsync();
        await using var db = CreateContext();
        var service = Ratings(db, principal);
        await service.SaveRatingAsync("track", "song-1", 0, 1);
        await service.SaveRatingAsync("track", "song-2", 2, 1);
        Assert.Equal(2, (await db.Ratings.SingleAsync(rating => rating.ItemId == "song-2")).Position);
        await service.SaveRatingAsync("track", "song-1", 0, 2);
        db.ChangeTracker.Clear();
        var ratings = await db.Ratings.Include(rating => rating.Category).OrderBy(rating => rating.RankWithinAlbum).ToListAsync();
        Assert.Equal("song-2", ratings[0].ItemId);
        Assert.All(ratings, rating => Assert.Equal(1, rating.Position));
        Assert.Equal(new int?[] { 1, 2 }, ratings.Select(rating => rating.RankWithinAlbum));
        await service.DeleteRatingAsync("track", "song-2");
        db.ChangeTracker.Clear();
        Assert.Equal(1, (await db.Ratings.SingleAsync()).RankWithinAlbum);
        Assert.Equal(2, await db.CollectionEntries.CountAsync());
    }

    private static RatingServices Ratings(PrestigeContext db, ClaimsPrincipal principal) => new(db,
        NullLogger<RatingServices>.Instance, principal, new ConfigurationBuilder().Build());

    [PostgresFact]
    public async Task AlbumAndArtistScoresCreateCollectionsWithoutListeningAggregates()
    {
        var principal = await SeedRatingCatalogAsync();
        await using var db = CreateContext();
        var service = Ratings(db, principal);
        var album = await service.SaveRatingAsync("album", "album-1", 0, 1);
        var artist = await service.SaveRatingAsync("artist", "artist-1", 0, 2);
        Assert.Equal(10m, album.PersonalScore);
        Assert.Equal(6.7m, artist.PersonalScore);
        Assert.Equal(1, album.Position);
        Assert.Equal(1, artist.Position);
        Assert.Equal(2, await db.CollectionEntries.CountAsync());
        Assert.Empty(await db.UserAlbums.ToListAsync());
        Assert.Empty(await db.UserArtists.ToListAsync());
    }

    private async Task<ClaimsPrincipal> SeedRatingCatalogAsync()
    {
        await using var db = CreateContext();
        await db.Database.MigrateAsync();
        var artist = new Artist("artist-1", "Artist", []);
        var album = new Album("album-1", "Album", [], [artist]);
        db.Tracks.AddRange(
            new Track(new TrackResponse { Id = "song-1", Name = "First song", DurationMs = 180000 }, album, [artist]),
            new Track(new TrackResponse { Id = "song-2", Name = "Second song", DurationMs = 180000 }, album, [artist]));
        await db.SaveChangesAsync();
        return await new CatalogAccountTransformation(db, new ConfigurationBuilder().Build()).TransformAsync(
            new ClaimsPrincipal(new ClaimsIdentity(
                [new(ClaimTypes.NameIdentifier, "auth0|listener"), new("iss", "https://accounts.example.com/")], "Test")));
    }

    private sealed class FailSecondSave : SaveChangesInterceptor
    {
        private int saves;
        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData,
            InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            if (++saves == 2) throw new InvalidOperationException("Simulated failure after an intermediate save.");
            return ValueTask.FromResult(result);
        }
    }
}
