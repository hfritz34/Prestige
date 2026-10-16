using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Npgsql;
using Prestige.Api.Auth;
using Prestige.Api.Domain;
using Prestige.Api.Endpoints.Collection;

namespace Prestige.Api.Tests;

public sealed partial class PostgresDatabaseTests
{
    [PostgresFact]
    public async Task ConcurrentCollectionSavesAreIdempotentWithoutListeningRows()
    {
        await using var db = CreateContext();
        await db.Database.MigrateAsync();
        db.Artists.Add(new Artist("artist-1", "Artist", []));
        await db.SaveChangesAsync();
        var configuration = new ConfigurationBuilder().Build();
        var principal = new ClaimsPrincipal(new ClaimsIdentity(
            [new(ClaimTypes.NameIdentifier, "auth0|collector"), new("iss", "https://accounts.example.com/")], "Test"));
        principal = await new CatalogAccountTransformation(db, configuration).TransformAsync(principal);

        var results = await Task.WhenAll(Enumerable.Range(0, 8).Select(async _ =>
        {
            await using var requestDb = CreateContext();
            return await new CollectionServices(requestDb, principal, configuration)
                .SaveAsync("artist", "artist-1", new CollectionUpdateRequest(true, true));
        }));
        Assert.All(results, result => Assert.True(result.IsFavorite && result.IsPinned));
        Assert.Single(results.Select(result => result.SavedAt).Distinct());
        Assert.Single(results.Select(result => result.UpdatedAt).Distinct());
        Assert.Single(await db.CollectionEntries.ToListAsync());
        Assert.Empty(await db.UserArtists.ToListAsync());
        Assert.Empty(await db.Ratings.ToListAsync());
        Assert.Equal(DateTimeKind.Utc, results[0].SavedAt.Kind);
    }

    [PostgresFact]
    public async Task CollectionDatabaseConstraintsRejectMissingOrMismatchedCatalogReferences()
    {
        await using var db = CreateContext();
        await db.Database.MigrateAsync();
        db.Users.Add(new User("collector", "Listener", "listener", "", "", "", ""));
        db.Artists.Add(new Artist("artist-1", "Artist", []));
        await db.SaveChangesAsync();

        db.CollectionEntries.Add(new CollectionEntry("collector", "artist", "missing"));
        var missing = await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
        Assert.Equal(PostgresErrorCodes.ForeignKeyViolation, Assert.IsType<PostgresException>(missing.InnerException).SqlState);
        db.ChangeTracker.Clear();

        var invalid = new CollectionEntry("collector", "artist", "artist-1");
        db.CollectionEntries.Add(invalid);
        db.Entry(invalid).Property(entry => entry.ArtistId).CurrentValue = null;
        var mismatched = await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
        Assert.Equal(PostgresErrorCodes.CheckViolation, Assert.IsType<PostgresException>(mismatched.InnerException).SqlState);
        db.ChangeTracker.Clear();
        Assert.Empty(await db.CollectionEntries.ToListAsync());
    }

    [PostgresFact]
    public async Task ConcurrentPartialUpdatesPreserveIndependentFlags()
    {
        var principal = await SeedRatingCatalogAsync();
        var configuration = new ConfigurationBuilder().Build();
        await Task.WhenAll(new[] { new CollectionPatchRequest(IsFavorite: true), new CollectionPatchRequest(IsPinned: true) }
            .Select(async patch =>
            {
                await using var db = CreateContext();
                return await new CollectionServices(db, principal, configuration).PatchAsync("track", "song-1", patch);
            }));
        await using var verification = CreateContext();
        var service = new CollectionServices(verification, principal, configuration);
        var entry = await service.GetAsync("track", "song-1");
        Assert.True(entry.IsFavorite && entry.IsPinned);
        entry = await service.PatchAsync("track", "song-1", new CollectionPatchRequest(IsPinned: false));
        Assert.True(entry.IsFavorite);
        Assert.False(entry.IsPinned);
        Assert.Single(await verification.CollectionEntries.ToListAsync());
    }
}
