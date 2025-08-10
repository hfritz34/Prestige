using System;
using System.Security.Claims;
using System.Linq;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Prestige.Api.Data;

namespace Prestige.Api.Endpoints.Search
{
    public class SearchServices : Endpoints.BaseService
    {
        public SearchServices(PrestigeContext db, ILogger<SearchServices> logger, ClaimsPrincipal principal, IConfiguration config)
            : base(db, logger, principal, config) { }

        public async Task<UserLibrarySearchResult> SearchUserLibraryAsync(string query, string type, int page, int pageSize)
        {
            var userId = GetUserId();
            var q = query.Trim();

            var items = new List<UserLibrarySearchItem>();

            // Load user totals for tie-break ordering
            var trackTotals = await PrestigeDb.UserTracks
                .Where(ut => ut.User.Id == userId)
                .Select(ut => new { ut.Track.Id, ut.TotalTime })
                .ToDictionaryAsync(x => x.Id, x => x.TotalTime);

            var albumTotals = await PrestigeDb.UserAlbums
                .Where(ua => ua.User.Id == userId)
                .Select(ua => new { ua.Album.Id, ua.TotalTime })
                .ToDictionaryAsync(x => x.Id, x => x.TotalTime);

            var artistTotals = await PrestigeDb.UserArtists
                .Where(ua => ua.User.Id == userId)
                .Select(ua => new { ua.Artist.Id, ua.TotalTime })
                .ToDictionaryAsync(x => x.Id, x => x.TotalTime);

            if (type is "track" or "all")
            {
                var tracks = PrestigeDb.Tracks
                    .Include(t => t.Artists)
                    .Include(t => t.Album)
                        .ThenInclude(a => a.Images)
                    .Where(t =>
                        EF.Functions.Like(t.Name, $"%{q}%") ||
                        t.Artists.Any(a => EF.Functions.Like(a.Name, $"%{q}%")) ||
                        (t.Album != null && EF.Functions.Like(t.Album.Name, $"%{q}%")))
                    .Where(t => PrestigeDb.UserTracks.Any(ut => ut.User.Id == userId && ut.Track.Id == t.Id));

                items.AddRange(await tracks
                    .Select(t => new UserLibrarySearchItem
                    {
                        Id = t.Id,
                        Name = t.Name,
                        ImageUrl = t.Album != null 
                            ? t.Album.Images
                                .OrderByDescending(img => img.Height)
                                .Select(img => img.Url)
                                .FirstOrDefault()
                            : null,
                        Artists = t.Artists.Select(a => a.Name).ToList(),
                        AlbumName = t.Album != null ? t.Album.Name : null,
                        ItemType = "track"
                    })
                    .ToListAsync());
            }

            if (type is "album" or "all")
            {
                var albums = PrestigeDb.Albums
                    .Include(a => a.Artists)
                    .Include(a => a.Images)
                    .Where(a =>
                        EF.Functions.Like(a.Name, $"%{q}%") ||
                        a.Artists.Any(ar => EF.Functions.Like(ar.Name, $"%{q}%")))
                    .Where(a => PrestigeDb.UserAlbums.Any(ua => ua.User.Id == userId && ua.Album.Id == a.Id));

                items.AddRange(await albums
                    .Select(a => new UserLibrarySearchItem
                    {
                        Id = a.Id,
                        Name = a.Name,
                        ImageUrl = a.Images
                            .OrderByDescending(img => img.Height)
                            .Select(img => img.Url)
                            .FirstOrDefault(),
                        Artists = a.Artists.Select(ar => ar.Name).ToList(),
                        AlbumName = null,
                        ItemType = "album"
                    })
                    .ToListAsync());
            }

            if (type is "artist" or "all")
            {
                var artists = PrestigeDb.Artists
                    .Include(ar => ar.Images)
                    .Where(ar => EF.Functions.Like(ar.Name, $"%{q}%"))
                    .Where(ar => PrestigeDb.UserArtists.Any(ua => ua.User.Id == userId && ua.Artist.Id == ar.Id));

                items.AddRange(await artists
                    .Select(ar => new UserLibrarySearchItem
                    {
                        Id = ar.Id,
                        Name = ar.Name,
                        ImageUrl = ar.Images
                            .OrderByDescending(img => img.Height)
                            .Select(img => img.Url)
                            .FirstOrDefault(),
                        Artists = new List<string>(),
                        AlbumName = null,
                        ItemType = "artist"
                    })
                    .ToListAsync());
            }

            int GetTotalTime(UserLibrarySearchItem i)
            {
                return i.ItemType switch
                {
                    "track" => trackTotals.TryGetValue(i.Id, out var tt) ? tt : 0,
                    "album" => albumTotals.TryGetValue(i.Id, out var at) ? at : 0,
                    "artist" => artistTotals.TryGetValue(i.Id, out var art) ? art : 0,
                    _ => 0
                };
            }

            var ordered = items
                .OrderByDescending(i => i.Name.StartsWith(q, StringComparison.InvariantCultureIgnoreCase))
                .ThenByDescending(i => i.Name.Contains(q, StringComparison.InvariantCultureIgnoreCase))
                .ThenByDescending(i => GetTotalTime(i))
                .ThenBy(i => i.ItemType)
                .ToList();

            var total = ordered.Count;
            var skip = Math.Max(0, (page - 1) * pageSize);
            var pageItems = ordered.Skip(skip).Take(pageSize).ToList();

            return new UserLibrarySearchResult
            {
                Total = total,
                Page = page,
                PageSize = pageSize,
                Items = pageItems
            };
        }

        private string GetUserId()
        {
            return Principal.FindFirst(ClaimTypes.NameIdentifier)?.Value?.Split("|").Last()
                   ?? throw new UnauthorizedAccessException("User ID not found");
        }
    }
}


