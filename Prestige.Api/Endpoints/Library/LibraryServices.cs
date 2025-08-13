using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Prestige.Api.Data;
using Prestige.Api.Endpoints;
using Prestige.Api.Endpoints.Library.RequestResponse;
using Prestige.Api.Exceptions;

namespace Prestige.Api.Endpoints.Library
{
    public class LibraryServices : BaseService
    {
        public LibraryServices(PrestigeContext db, ILogger<LibraryServices> logger, ClaimsPrincipal principal, IConfiguration config) 
            : base(db, logger, principal, config)
        {
        }

        public async Task<ItemDetailsResponse> GetItemDetailsAsync(string itemType, string itemId)
        {
            var userId = GetUserId();
            var normalizedType = itemType.ToLowerInvariant();

            switch (normalizedType)
            {
                case "track":
                    return await GetTrackDetailsAsync(userId, itemId);
                case "album":
                    return await GetAlbumDetailsAsync(userId, itemId);
                case "artist":
                    return await GetArtistDetailsAsync(userId, itemId);
                default:
                    throw new System.ArgumentException($"Invalid item type: {itemType}");
            }
        }

        public async Task<List<ItemDetailsResponse>> GetItemDetailsBatchAsync(List<ItemRequest> items)
        {
            var userId = GetUserId();
            
            // Group items by type for efficient queries
            var trackIds = items.Where(i => i.Type.ToLowerInvariant() == "track").Select(i => i.Id).ToList();
            var albumIds = items.Where(i => i.Type.ToLowerInvariant() == "album").Select(i => i.Id).ToList();
            var artistIds = items.Where(i => i.Type.ToLowerInvariant() == "artist").Select(i => i.Id).ToList();
            
            var tasks = new List<Task<List<ItemDetailsResponse>>>();
            
            if (trackIds.Any())
                tasks.Add(GetTrackDetailsBatchAsync(userId, trackIds));
            if (albumIds.Any())
                tasks.Add(GetAlbumDetailsBatchAsync(userId, albumIds));
            if (artistIds.Any())
                tasks.Add(GetArtistDetailsBatchAsync(userId, artistIds));
            
            var results = await Task.WhenAll(tasks);
            return results.SelectMany(r => r).ToList();
        }

        private string GetUserId()
        {
            return Principal.FindFirst(ClaimTypes.NameIdentifier)?.Value?.Split("|").Last() 
                ?? throw new UnauthorizedAccessException("User ID not found");
        }

        private async Task<ItemDetailsResponse> GetTrackDetailsAsync(string userId, string itemId)
        {
            // First try to find in user's tracks
            var userTrack = await PrestigeDb.UserTracks
                .Include(ut => ut.Track)
                .ThenInclude(t => t.Album)
                .ThenInclude(a => a.Images)
                .Include(ut => ut.Track)
                .ThenInclude(t => t.Artists)
                .Where(ut => ut.User.Id == userId && ut.Track.Id == itemId)
                .FirstOrDefaultAsync();

            if (userTrack != null)
            {
                var track = userTrack.Track;
                return new ItemDetailsResponse
                {
                    Id = track.Id,
                    Name = track.Name,
                    ItemType = "track",
                    ImageUrl = track.Album?.Images?.FirstOrDefault()?.Url,
                    Artists = track.Artists?.Select(a => a.Name).ToList(),
                    AlbumName = track.Album?.Name
                };
            }

            // If not found in user tracks, try to find in general tracks table
            var track2 = await PrestigeDb.Tracks
                .Include(t => t.Album)
                .ThenInclude(a => a.Images)
                .Include(t => t.Artists)
                .Where(t => t.Id == itemId)
                .FirstOrDefaultAsync();

            if (track2 != null)
            {
                return new ItemDetailsResponse
                {
                    Id = track2.Id,
                    Name = track2.Name,
                    ItemType = "track",
                    ImageUrl = track2.Album?.Images?.FirstOrDefault()?.Url,
                    Artists = track2.Artists?.Select(a => a.Name).ToList(),
                    AlbumName = track2.Album?.Name
                };
            }

            throw new NotFoundException(404, $"Track {itemId} not found");
        }

        private async Task<ItemDetailsResponse> GetAlbumDetailsAsync(string userId, string itemId)
        {
            // First try to find in user's albums
            var userAlbum = await PrestigeDb.UserAlbums
                .Include(ua => ua.Album)
                .ThenInclude(a => a.Images)
                .Include(ua => ua.Album)
                .ThenInclude(a => a.Artists)
                .Where(ua => ua.User.Id == userId && ua.Album.Id == itemId)
                .FirstOrDefaultAsync();

            if (userAlbum != null)
            {
                var album = userAlbum.Album;
                return new ItemDetailsResponse
                {
                    Id = album.Id,
                    Name = album.Name,
                    ItemType = "album",
                    ImageUrl = album.Images?.FirstOrDefault()?.Url,
                    Artists = album.Artists?.Select(a => a.Name).ToList(),
                    AlbumName = null
                };
            }

            // If not found in user albums, try to find in general albums table
            var album2 = await PrestigeDb.Albums
                .Include(a => a.Images)
                .Include(a => a.Artists)
                .Where(a => a.Id == itemId)
                .FirstOrDefaultAsync();

            if (album2 != null)
            {
                return new ItemDetailsResponse
                {
                    Id = album2.Id,
                    Name = album2.Name,
                    ItemType = "album",
                    ImageUrl = album2.Images?.FirstOrDefault()?.Url,
                    Artists = album2.Artists?.Select(a => a.Name).ToList(),
                    AlbumName = null
                };
            }

            throw new NotFoundException(404, $"Album {itemId} not found");
        }

        private async Task<ItemDetailsResponse> GetArtistDetailsAsync(string userId, string itemId)
        {
            // First try to find in user's artists
            var userArtist = await PrestigeDb.UserArtists
                .Include(ua => ua.Artist)
                .ThenInclude(a => a.Images)
                .Where(ua => ua.User.Id == userId && ua.Artist.Id == itemId)
                .FirstOrDefaultAsync();

            if (userArtist != null)
            {
                var artist = userArtist.Artist;
                return new ItemDetailsResponse
                {
                    Id = artist.Id,
                    Name = artist.Name,
                    ItemType = "artist",
                    ImageUrl = artist.Images?.FirstOrDefault()?.Url,
                    Artists = null,
                    AlbumName = null
                };
            }

            // If not found in user artists, try to find in general artists table
            var artist2 = await PrestigeDb.Artists
                .Include(a => a.Images)
                .Where(a => a.Id == itemId)
                .FirstOrDefaultAsync();

            if (artist2 != null)
            {
                return new ItemDetailsResponse
                {
                    Id = artist2.Id,
                    Name = artist2.Name,
                    ItemType = "artist",
                    ImageUrl = artist2.Images?.FirstOrDefault()?.Url,
                    Artists = null,
                    AlbumName = null
                };
            }

            throw new NotFoundException(404, $"Artist {itemId} not found");
        }

        private async Task<List<ItemDetailsResponse>> GetTrackDetailsBatchAsync(string userId, List<string> trackIds)
        {
            var results = new List<ItemDetailsResponse>();

            // First try to find in user's tracks
            var userTracks = await PrestigeDb.UserTracks
                .Include(ut => ut.Track)
                .ThenInclude(t => t.Album)
                .ThenInclude(a => a.Images)
                .Include(ut => ut.Track)
                .ThenInclude(t => t.Artists)
                .Where(ut => ut.User.Id == userId && trackIds.Contains(ut.Track.Id))
                .ToListAsync();

            foreach (var userTrack in userTracks)
            {
                var track = userTrack.Track;
                results.Add(new ItemDetailsResponse
                {
                    Id = track.Id,
                    Name = track.Name,
                    ItemType = "track",
                    ImageUrl = track.Album?.Images?.FirstOrDefault()?.Url,
                    Artists = track.Artists?.Select(a => a.Name).ToList(),
                    AlbumName = track.Album?.Name
                });
            }

            // Find remaining tracks not in user's collection
            var foundIds = userTracks.Select(ut => ut.Track.Id).ToList();
            var remainingIds = trackIds.Except(foundIds).ToList();

            if (remainingIds.Any())
            {
                var generalTracks = await PrestigeDb.Tracks
                    .Include(t => t.Album)
                    .ThenInclude(a => a.Images)
                    .Include(t => t.Artists)
                    .Where(t => remainingIds.Contains(t.Id))
                    .ToListAsync();

                foreach (var track in generalTracks)
                {
                    results.Add(new ItemDetailsResponse
                    {
                        Id = track.Id,
                        Name = track.Name,
                        ItemType = "track",
                        ImageUrl = track.Album?.Images?.FirstOrDefault()?.Url,
                        Artists = track.Artists?.Select(a => a.Name).ToList(),
                        AlbumName = track.Album?.Name
                    });
                }
            }

            return results;
        }

        private async Task<List<ItemDetailsResponse>> GetAlbumDetailsBatchAsync(string userId, List<string> albumIds)
        {
            var results = new List<ItemDetailsResponse>();

            // First try to find in user's albums
            var userAlbums = await PrestigeDb.UserAlbums
                .Include(ua => ua.Album)
                .ThenInclude(a => a.Images)
                .Include(ua => ua.Album)
                .ThenInclude(a => a.Artists)
                .Where(ua => ua.User.Id == userId && albumIds.Contains(ua.Album.Id))
                .ToListAsync();

            foreach (var userAlbum in userAlbums)
            {
                var album = userAlbum.Album;
                results.Add(new ItemDetailsResponse
                {
                    Id = album.Id,
                    Name = album.Name,
                    ItemType = "album",
                    ImageUrl = album.Images?.FirstOrDefault()?.Url,
                    Artists = album.Artists?.Select(a => a.Name).ToList(),
                    AlbumName = null
                });
            }

            // Find remaining albums not in user's collection
            var foundIds = userAlbums.Select(ua => ua.Album.Id).ToList();
            var remainingIds = albumIds.Except(foundIds).ToList();

            if (remainingIds.Any())
            {
                var generalAlbums = await PrestigeDb.Albums
                    .Include(a => a.Images)
                    .Include(a => a.Artists)
                    .Where(a => remainingIds.Contains(a.Id))
                    .ToListAsync();

                foreach (var album in generalAlbums)
                {
                    results.Add(new ItemDetailsResponse
                    {
                        Id = album.Id,
                        Name = album.Name,
                        ItemType = "album",
                        ImageUrl = album.Images?.FirstOrDefault()?.Url,
                        Artists = album.Artists?.Select(a => a.Name).ToList(),
                        AlbumName = null
                    });
                }
            }

            return results;
        }

        private async Task<List<ItemDetailsResponse>> GetArtistDetailsBatchAsync(string userId, List<string> artistIds)
        {
            var results = new List<ItemDetailsResponse>();

            // First try to find in user's artists
            var userArtists = await PrestigeDb.UserArtists
                .Include(ua => ua.Artist)
                .ThenInclude(a => a.Images)
                .Where(ua => ua.User.Id == userId && artistIds.Contains(ua.Artist.Id))
                .ToListAsync();

            foreach (var userArtist in userArtists)
            {
                var artist = userArtist.Artist;
                results.Add(new ItemDetailsResponse
                {
                    Id = artist.Id,
                    Name = artist.Name,
                    ItemType = "artist",
                    ImageUrl = artist.Images?.FirstOrDefault()?.Url,
                    Artists = null,
                    AlbumName = null
                });
            }

            // Find remaining artists not in user's collection
            var foundIds = userArtists.Select(ua => ua.Artist.Id).ToList();
            var remainingIds = artistIds.Except(foundIds).ToList();

            if (remainingIds.Any())
            {
                var generalArtists = await PrestigeDb.Artists
                    .Include(a => a.Images)
                    .Where(a => remainingIds.Contains(a.Id))
                    .ToListAsync();

                foreach (var artist in generalArtists)
                {
                    results.Add(new ItemDetailsResponse
                    {
                        Id = artist.Id,
                        Name = artist.Name,
                        ItemType = "artist",
                        ImageUrl = artist.Images?.FirstOrDefault()?.Url,
                        Artists = null,
                        AlbumName = null
                    });
                }
            }

            return results;
        }
    }
}