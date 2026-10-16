using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Prestige.Api.Data;
using Prestige.Api.Domain;
using Prestige.Api.Endpoints.Spotify.RequestResponse;

namespace Prestige.Api.Tests;

/// <summary>Each test owns a disposable database on the configured PostgreSQL test server.</summary>
public sealed class PostgresDatabaseTests : IAsyncLifetime
{
    private readonly string databaseName = $"prestige_test_{Guid.NewGuid():N}";
    private string adminConnection = "";
    private string connectionString = "";
    private bool databaseCreated;

    public async Task InitializeAsync()
    {
        var configured = Environment.GetEnvironmentVariable("PRESTIGE_TEST_POSTGRES");
        if (string.IsNullOrWhiteSpace(configured)) return;
        var settings = new NpgsqlConnectionStringBuilder(configured) { Pooling = false };
        adminConnection = settings.ConnectionString;
        await using var connection = new NpgsqlConnection(adminConnection);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand($"CREATE DATABASE {databaseName}", connection);
        await command.ExecuteNonQueryAsync();
        databaseCreated = true;
        settings.Database = databaseName;
        connectionString = settings.ConnectionString;
    }

    public async Task DisposeAsync()
    {
        if (!databaseCreated) return;
        await using var connection = new NpgsqlConnection(adminConnection);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand($"DROP DATABASE {databaseName} WITH (FORCE)", connection);
        await command.ExecuteNonQueryAsync();
    }

    [PostgresFact]
    public async Task FreshBaselineMigratesAndPreservesRatingsAndUtcDates()
    {
        await using var db = CreateContext();
        var migrations = db.Database.GetMigrations().ToList();
        Assert.Contains(migrations, name => name.EndsWith("_CatalogBaseline"));
        await using var legacy = new PrestigeContext(new DbContextOptionsBuilder<PrestigeContext>()
            .UseSqlServer("Server=unused.invalid;Database=legacy;User Id=test;Password=test").Options);
        Assert.Empty(migrations.Intersect(legacy.Database.GetMigrations()));
        Assert.DoesNotContain(migrations, name => name.Contains("Nuke"));

        await db.Database.MigrateAsync();
        Assert.False(db.Database.HasPendingModelChanges());
        Assert.Equal(3, await db.RatingCategories.CountAsync());

        var user = new User("catalog-user", "Music Fan", "fan", "fan@example.com", "", "", "");
        var artist = new Artist("artist-1", "Artist", []);
        var album = new Album("album-1", "Album", [], [artist]);
        var track = new Track(new TrackResponse { Id = "track-1", Name = "Song", DurationMs = 180000 }, album, [artist]);
        db.Users.Add(user);
        db.Tracks.Add(track);
        var rating = new Rating(user, track.Id, "track", await db.RatingCategories.FindAsync(1)
            ?? throw new InvalidOperationException("Rating categories were not seeded"), 1, 10m, album.Id);
        rating.UpdateRankWithinAlbum(1);
        db.Ratings.Add(rating);
        db.UserConsents.Add(new UserConsent(user.Id, "catalog-v1"));
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var saved = await db.Ratings.Include(r => r.User).SingleAsync();
        Assert.Equal(1, saved.RankWithinAlbum);
        Assert.Equal("album-1", saved.AlbumId);
        Assert.Equal(10m, saved.PersonalScore);
        Assert.Equal(DateTimeKind.Utc, saved.CreatedAt.Kind);
        Assert.Equal(DateTimeKind.Utc, saved.User.ExpiresAt.Kind);
        Assert.Equal(4, await db.ConsentEvents.CountAsync());

        // Applying the baseline again must be a no-op, not recreate or reseed existing data.
        await db.Database.MigrateAsync();
        Assert.Single(await db.Ratings.ToListAsync());
        Assert.Equal(3, await db.RatingCategories.CountAsync());
        Assert.Empty(await db.Database.GetPendingMigrationsAsync());
    }

    [PostgresFact]
    public async Task ActualCatalogHostUsesPostgresAndReportsReadinessAfterMigration()
    {
        await using var app = Program.CreateApplication([], builder =>
        {
            builder.Environment.EnvironmentName = "Production";
            builder.Configuration.Sources.Clear();
            builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Postgres"] = connectionString,
                ["Cors:AllowedOrigins:0"] = "https://music.example.com",
                ["Auth0:Domain"] = "example.auth0.com",
                ["Auth0:Audience"] = "https://api.example.com"
            });
            builder.WebHost.UseTestServer();
        });
        await app.StartAsync();
        var client = app.GetTestClient();
        Assert.Equal(HttpStatusCode.ServiceUnavailable, (await client.GetAsync("/api/ready")).StatusCode);
        using (var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PrestigeContext>();
            Assert.IsType<PostgresPrestigeContext>(db);
            Assert.Same(db, scope.ServiceProvider.GetRequiredService<PostgresPrestigeContext>());
            await db.Database.MigrateAsync();
        }
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/ready")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("api/rating/categories")).StatusCode);
    }

    private PostgresPrestigeContext CreateContext() => new(new DbContextOptionsBuilder<PostgresPrestigeContext>()
        .UseNpgsql(connectionString, options => options.SetPostgresVersion(15, 0)).Options);

    [PostgresFact]
    public async Task ConcurrentFirstLoginsCreateOneAccountAndConsentHistory()
    {
        await using var db = CreateContext();
        await db.Database.MigrateAsync();
        var configuration = new ConfigurationBuilder().Build();
        var ids = await Task.WhenAll(Enumerable.Range(0, 8).Select(async _ =>
        {
            await using var requestDb = CreateContext();
            var principal = new System.Security.Claims.ClaimsPrincipal(new System.Security.Claims.ClaimsIdentity(
                [new(System.Security.Claims.ClaimTypes.NameIdentifier, "auth0|new-listener"),
                 new("iss", "https://accounts.example.com/")], "Test"));
            var transformed = await new Auth.CatalogAccountTransformation(requestDb, configuration).TransformAsync(principal);
            return Auth.CurrentAccount.GetUserId(transformed, configuration);
        }));

        Assert.NotNull(ids[0]);
        Assert.Single(ids.Distinct());
        Assert.Single(await db.Users.ToListAsync());
        Assert.Single(await db.UserIdentities.ToListAsync());
        Assert.Single(await db.UserConsents.ToListAsync());
        Assert.Equal(4, await db.ConsentEvents.CountAsync());
    }
}

public sealed class PostgresFactAttribute : FactAttribute
{
    public PostgresFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("PRESTIGE_TEST_POSTGRES")))
            Skip = "Set PRESTIGE_TEST_POSTGRES to run real PostgreSQL migration and persistence tests (required in CI).";
    }
}
