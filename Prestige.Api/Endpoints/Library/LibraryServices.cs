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
    }
}