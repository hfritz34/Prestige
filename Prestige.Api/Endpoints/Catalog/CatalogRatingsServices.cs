using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Prestige.Api.Auth;
using Prestige.Api.Data;
using Prestige.Api.Endpoints.Collection;
using Prestige.Api.Endpoints.Rating.RequestResponse;
using Prestige.Api.Endpoints.Spotify.RequestResponse;

namespace Prestige.Api.Endpoints.Catalog;

public sealed record CatalogRatingResponse(RatingResponse Rating, TrackResponse? Track = null,
    AlbumResponse? Album = null, ArtistResponse? Artist = null);

public sealed class CatalogRatingsServices(PrestigeContext db, ClaimsPrincipal principal, IConfiguration configuration)
{
    public async Task<List<CatalogRatingResponse>> ListAsync(string itemType, int skip = 0, int take = 50,
        CancellationToken cancellationToken = default)
    {
        var type = CollectionServices.NormalizeType(itemType);
        if (skip < 0 || take is < 1 or > 100)
            throw new Exceptions.ArgumentException(400, "skip must be nonnegative and take must be between 1 and 100.");
        var userId = CurrentAccount.GetUserId(principal, configuration)
            ?? throw new Exceptions.UnauthorizedException(401, "An authenticated account is required.");
        var ratings = await db.Ratings.AsNoTracking()
            .Where(r => r.User.Id == userId && r.ItemType == type)
            .OrderBy(r => r.Category.DisplayOrder).ThenBy(r => r.Position).ThenBy(r => r.ItemId)
            .Skip(skip).Take(take)
            .Select(r => new RatingResponse {
                ItemId = r.ItemId, ItemType = r.ItemType, CategoryId = r.Category.Id,
                PersonalScore = type == "track" ? null : r.PersonalScore, Position = r.Position,
                RankWithinAlbum = r.RankWithinAlbum, AlbumId = r.AlbumId, IsNewRating = false
            }).ToListAsync(cancellationToken);
        if (ratings.Count == 0) return [];
        var ids = ratings.Select(r => r.ItemId).ToList();
        // Load metadata in a bounded batch, independent of whether a rating is still saved in the collection.
        if (type == "track")
        {
            var items = await db.Tracks.AsNoTracking().Where(t => ids.Contains(t.Id))
                .Include(t => t.Artists).ThenInclude(a => a.Images)
                .Include(t => t.Album).ThenInclude(a => a.Images)
                .Include(t => t.Album).ThenInclude(a => a.Artists).ThenInclude(a => a.Images)
                .AsSplitQuery().ToDictionaryAsync(t => t.Id, cancellationToken);
            return ratings.Select(r => new CatalogRatingResponse(r,
                Track: items.TryGetValue(r.ItemId, out var item) ? new TrackResponse(item) : null)).ToList();
        }
        if (type == "album")
        {
            var items = await db.Albums.AsNoTracking().Where(a => ids.Contains(a.Id))
                .Include(a => a.Images).Include(a => a.Artists).ThenInclude(a => a.Images)
                .AsSplitQuery().ToDictionaryAsync(a => a.Id, cancellationToken);
            return ratings.Select(r => new CatalogRatingResponse(r,
                Album: items.TryGetValue(r.ItemId, out var item) ? new AlbumResponse(item) : null)).ToList();
        }
        var artists = await db.Artists.AsNoTracking().Where(a => ids.Contains(a.Id)).Include(a => a.Images)
            .AsSplitQuery().ToDictionaryAsync(a => a.Id, cancellationToken);
        return ratings.Select(r => new CatalogRatingResponse(r,
            Artist: artists.TryGetValue(r.ItemId, out var artist) ? new ArtistResponse(artist) : null)).ToList();
    }
}
