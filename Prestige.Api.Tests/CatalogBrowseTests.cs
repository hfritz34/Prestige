using System.Security.Claims;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Prestige.Api.Data;
using Prestige.Api.Domain;
using Prestige.Api.Endpoints.Prestige;

namespace Prestige.Api.Tests;

public sealed partial class CatalogAccountTests
{
    [Fact]
    public async Task CatalogDetailReadsDoNotAttemptSpotifyEnrichment()
    {
        await SeedCatalogAsync();
        var client = Client("auth0|browser");
        var id = (await Me(client)).GetProperty("id").GetString()!;
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PrestigeContext>();
        var album = new Album("album-without-tracks", "Not imported yet", [], [await db.Artists.SingleAsync()]);
        db.Albums.Add(album);
        db.Ratings.Add(new Rating(await db.Users.SingleAsync(), album.Id, "album",
            (await db.RatingCategories.FindAsync(1))!, 1, 8m));
        await db.SaveChangesAsync();
        var logger = new BrowseLogger();
        // Any access to the absent Spotify dependency would be caught and logged by the legacy fallback.
        var service = new PrestigeServices(db, logger, new ClaimsPrincipal(), new ConfigurationBuilder().Build(), null!);
        var tracks = JsonSerializer.SerializeToElement(await service.GetAlbumTracksWithRankings(id, "album-1"));
        Assert.Equal(2, tracks.GetProperty("TotalTracks").GetInt32());
        var albums = JsonSerializer.SerializeToElement(await service.GetArtistAlbumsWithUserActivity(id, "artist-1"));
        Assert.Equal(0, Assert.Single(albums.GetProperty("Albums").EnumerateArray()).GetProperty("TotalTracks").GetInt32());
        Assert.Empty(logger.Messages);
        Assert.Empty(await db.UserTracks.ToListAsync());
    }

    private sealed class BrowseLogger : ILogger<PrestigeServices>
    {
        public List<string> Messages { get; } = [];
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter) => Messages.Add(formatter(state, exception));
    }
}
