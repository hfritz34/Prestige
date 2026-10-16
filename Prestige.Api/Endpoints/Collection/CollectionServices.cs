using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Prestige.Api.Auth;
using Prestige.Api.Data;
using Prestige.Api.Domain;
using Prestige.Api.Exceptions;
using Prestige.Api.Endpoints.Spotify.RequestResponse;

namespace Prestige.Api.Endpoints.Collection;

public sealed record CollectionUpdateRequest(bool IsFavorite = false, bool IsPinned = false);
public sealed record CollectionPatchRequest(bool? IsFavorite = null, bool? IsPinned = null);
public sealed record CollectionRating(int CategoryId, decimal? Score, int Position, int? RankWithinAlbum);
public sealed record CollectionResponse(string ItemType, string ItemId, string Name, string? ImageUrl,
    List<string> Artists, string? AlbumId, bool IsFavorite, bool IsPinned, DateTime SavedAt,
    DateTime UpdatedAt, CollectionRating? Rating)
{
    public TrackResponse? Track { get; init; }
    public AlbumResponse? Album { get; init; }
    public ArtistResponse? Artist { get; init; }
}

public sealed class CollectionServices(PrestigeContext db, ClaimsPrincipal principal, IConfiguration configuration)
{
    private string UserId => CurrentAccount.GetUserId(principal, configuration)
        ?? throw new UnauthorizedException(401, "An authenticated account is required.");

    public static string NormalizeType(string itemType)
    {
        var normalized = itemType.Trim().ToLowerInvariant();
        if (normalized is not ("track" or "album" or "artist"))
            throw new Exceptions.ArgumentException(400, "Item type must be track, album or artist.");
        return normalized;
    }

    public async Task<List<CollectionResponse>> ListAsync(string? itemType, bool favoritesOnly,
        bool pinnedOnly, int skip, int take, CancellationToken cancellationToken = default)
    {
        if (skip < 0 || take is < 1 or > 100)
            throw new Exceptions.ArgumentException(400, "Use skip >= 0 and take between 1 and 100.");
        var userId = UserId;
        var query = db.CollectionEntries.AsNoTracking().Where(entry => entry.UserId == userId);
        if (itemType != null)
        {
            var type = NormalizeType(itemType);
            query = query.Where(entry => entry.ItemType == type);
        }
        if (favoritesOnly) query = query.Where(entry => entry.IsFavorite);
        if (pinnedOnly) query = query.Where(entry => entry.IsPinned);
        var entries = await WithMetadata(query.OrderByDescending(entry => entry.UpdatedAt)
            .ThenBy(entry => entry.ItemType).ThenBy(entry => entry.ItemId).Skip(skip).Take(take))
            .ToListAsync(cancellationToken);
        return await ResponsesAsync(userId, entries, cancellationToken);
    }

    public async Task<CollectionResponse> GetAsync(string itemType, string itemId, CancellationToken cancellationToken = default)
    {
        var type = NormalizeType(itemType);
        var userId = UserId;
        var entry = await WithMetadata(db.CollectionEntries.AsNoTracking()
            .Where(entry => entry.UserId == userId && entry.ItemType == type && entry.ItemId == itemId))
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException(404, "Item is not in your collection.");
        return (await ResponsesAsync(userId, [entry], cancellationToken)).Single();
    }

    public async Task<CollectionResponse> SaveAsync(string itemType, string itemId, CollectionUpdateRequest request,
        CancellationToken cancellationToken = default)
        => await PatchAsync(itemType, itemId, new CollectionPatchRequest(request.IsFavorite, request.IsPinned), cancellationToken);

    public async Task<CollectionResponse> PatchAsync(string itemType, string itemId, CollectionPatchRequest request,
        CancellationToken cancellationToken = default)
    {
        var type = NormalizeType(itemType);
        var userId = UserId;
        await using var transaction = await AccountWriteTransaction.BeginAsync(db, userId, cancellationToken);
        var entry = await EnsureSavedAsync(db, userId, type, itemId, cancellationToken);
        entry.Update(request.IsFavorite ?? entry.IsFavorite, request.IsPinned ?? entry.IsPinned);
        await db.SaveChangesAsync(cancellationToken);
        var response = await GetAsync(type, itemId, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return response;
    }

    public async Task RemoveAsync(string itemType, string itemId, CancellationToken cancellationToken = default)
    {
        var type = NormalizeType(itemType);
        var userId = UserId;
        await using var transaction = await AccountWriteTransaction.BeginAsync(db, userId, cancellationToken);
        var entry = await db.CollectionEntries.FindAsync([userId, type, itemId], cancellationToken);
        if (entry != null) db.CollectionEntries.Remove(entry);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    // Call within the account's write transaction; it deliberately does not save or commit independently.
    public static async Task<CollectionEntry> EnsureSavedAsync(PrestigeContext db, string userId,
        string itemType, string itemId, CancellationToken cancellationToken = default)
    {
        var type = NormalizeType(itemType);
        if (string.IsNullOrWhiteSpace(itemId) || itemId.Length > 50)
            throw new Exceptions.ArgumentException(400, "A catalog item ID of at most 50 characters is required.");
        var existing = await db.CollectionEntries.FindAsync([userId, type, itemId], cancellationToken);
        if (existing != null) return existing;
        await RequireKnownItemAsync(db, type, itemId, cancellationToken);
        var entry = new CollectionEntry(userId, type, itemId);
        db.CollectionEntries.Add(entry);
        return entry;
    }

    public static async Task RequireKnownItemAsync(PrestigeContext db, string itemType, string itemId,
        CancellationToken cancellationToken = default)
    {
        var type = NormalizeType(itemType);
        if (string.IsNullOrWhiteSpace(itemId) || itemId.Length > 50)
            throw new Exceptions.ArgumentException(400, "A catalog item ID of at most 50 characters is required.");
        var exists = type switch
        {
            "track" => await db.Tracks.AnyAsync(item => item.Id == itemId, cancellationToken),
            "album" => await db.Albums.AnyAsync(item => item.Id == itemId, cancellationToken),
            _ => await db.Artists.AnyAsync(item => item.Id == itemId, cancellationToken)
        };
        if (!exists) throw new NotFoundException(404, "Music must exist in the catalog before it can be saved.");
    }

    private static IQueryable<CollectionEntry> WithMetadata(IQueryable<CollectionEntry> query) => query
        .Include(entry => entry.Track!).ThenInclude(track => track.Album).ThenInclude(album => album.Images)
        .Include(entry => entry.Track!).ThenInclude(track => track.Album).ThenInclude(album => album.Artists).ThenInclude(artist => artist.Images)
        .Include(entry => entry.Track!).ThenInclude(track => track.Artists).ThenInclude(artist => artist.Images)
        .Include(entry => entry.Album!).ThenInclude(album => album.Images)
        .Include(entry => entry.Album!).ThenInclude(album => album.Artists).ThenInclude(artist => artist.Images)
        .Include(entry => entry.Artist!).ThenInclude(artist => artist.Images)
        .AsSplitQuery();

    private async Task<List<CollectionResponse>> ResponsesAsync(string userId, List<CollectionEntry> entries,
        CancellationToken cancellationToken)
    {
        if (entries.Count == 0) return [];
        var ids = entries.Select(entry => entry.ItemId).ToArray();
        var ratings = await db.Ratings.AsNoTracking().Include(rating => rating.Category)
            .Where(rating => rating.User.Id == userId && ids.Contains(rating.ItemId)).ToListAsync(cancellationToken);
        var byItem = ratings.ToDictionary(rating => (rating.ItemType.ToLowerInvariant(), rating.ItemId));
        return entries.Select(entry =>
        {
            byItem.TryGetValue((entry.ItemType, entry.ItemId), out var rating);
            var album = entry.Track?.Album ?? entry.Album;
            return new CollectionResponse(entry.ItemType, entry.ItemId,
                entry.Track?.Name ?? entry.Album?.Name ?? entry.Artist!.Name,
                (album?.Images ?? entry.Artist?.Images)?.OrderBy(image => image.Url).FirstOrDefault()?.Url,
                (entry.Track?.Artists ?? entry.Album?.Artists)?.Select(artist => artist.Name).ToList() ?? [],
                entry.Track?.Album.Id, entry.IsFavorite, entry.IsPinned, entry.SavedAt, entry.UpdatedAt,
                rating == null ? null : new CollectionRating(rating.Category.Id,
                    entry.ItemType == "track" ? null : rating.PersonalScore, rating.Position, rating.RankWithinAlbum))
            {
                Track = entry.Track == null ? null : new TrackResponse(entry.Track),
                Album = entry.Album == null ? null : new AlbumResponse(entry.Album),
                Artist = entry.Artist == null ? null : new ArtistResponse(entry.Artist)
            };
        }).ToList();
    }
}
