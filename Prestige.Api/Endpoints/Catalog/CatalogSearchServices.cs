using Microsoft.EntityFrameworkCore;
using Prestige.Api.Data;
using Prestige.Api.Endpoints.Spotify.RequestResponse;
using ArgumentException = Prestige.Api.Exceptions.ArgumentException;

namespace Prestige.Api.Endpoints.Catalog;

public sealed record CatalogSearchResponse(List<TrackResponse> Tracks, List<AlbumResponse> Albums,
    List<ArtistResponse> Artists);

// Searches stored metadata only. Finding music must not create listening activity or call a provider.
public sealed class CatalogSearchServices(PrestigeContext db)
{
    public async Task<CatalogSearchResponse> SearchAsync(string? query, string? itemType = "all",
        int take = 20, CancellationToken cancellationToken = default)
    {
        var term = query?.Trim().ToLowerInvariant();
        if (string.IsNullOrEmpty(term) || term.Length > 120)
            throw new ArgumentException(400, "query must contain 1 to 120 characters.");
        var type = (itemType ?? "all").Trim().ToLowerInvariant();
        if (type is not ("all" or "track" or "album" or "artist"))
            throw new ArgumentException(400, "itemType must be track, album, artist, or all.");
        if (take is < 1 or > 50)
            throw new ArgumentException(400, "take must be between 1 and 50 per item type.");

        var result = new CatalogSearchResponse([], [], []);
        // Contains treats %, _ and backslashes literally, unlike an unescaped LIKE pattern.
        // Every query is bounded before related metadata is loaded; no catalog-wide materialization.
        if (type is "all" or "track")
        {
            var tracks = await db.Tracks.AsNoTracking()
                .Where(track => track.Name.ToLower().Contains(term)
                    || track.Album.Name.ToLower().Contains(term)
                    || track.Artists.Any(artist => artist.Name.ToLower().Contains(term)))
                .OrderByDescending(track => track.Name.ToLower().StartsWith(term))
                .ThenBy(track => track.Name.ToLower()).ThenBy(track => track.Id)
                .Take(take)
                .Include(track => track.Artists).ThenInclude(artist => artist.Images)
                .Include(track => track.Album).ThenInclude(album => album.Images)
                .Include(track => track.Album).ThenInclude(album => album.Artists).ThenInclude(artist => artist.Images)
                .AsSplitQuery().ToListAsync(cancellationToken);
            result.Tracks.AddRange(tracks.Select(track => new TrackResponse(track)));
        }
        if (type is "all" or "album")
        {
            var albums = await db.Albums.AsNoTracking()
                .Where(album => album.Name.ToLower().Contains(term)
                    || album.Artists.Any(artist => artist.Name.ToLower().Contains(term)))
                .OrderByDescending(album => album.Name.ToLower().StartsWith(term))
                .ThenBy(album => album.Name.ToLower()).ThenBy(album => album.Id)
                .Take(take)
                .Include(album => album.Images)
                .Include(album => album.Artists).ThenInclude(artist => artist.Images)
                .AsSplitQuery().ToListAsync(cancellationToken);
            result.Albums.AddRange(albums.Select(album => new AlbumResponse(album)));
        }
        if (type is "all" or "artist")
        {
            var artists = await db.Artists.AsNoTracking()
                .Where(artist => artist.Name.ToLower().Contains(term))
                .OrderByDescending(artist => artist.Name.ToLower().StartsWith(term))
                .ThenBy(artist => artist.Name.ToLower()).ThenBy(artist => artist.Id)
                .Take(take).Include(artist => artist.Images)
                .AsSplitQuery().ToListAsync(cancellationToken);
            result.Artists.AddRange(artists.Select(artist => new ArtistResponse(artist)));
        }
        return result;
    }
}
