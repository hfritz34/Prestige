using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;
using Exception = Prestige.Api.Exceptions.Exception;

namespace Prestige.Api.Endpoints.Profile
{
    [ApiController]
    [Authorize]
    [Route("profiles")]
    public class ProfileController : BaseApiController
    {
        private readonly ProfileServices _service;

        public ProfileController(ProfileServices service)
        {
            _service = service;
        }


        [HttpGet("{userId}/top/tracks")]
        public async Task<IActionResult> GetTopTracksAsync(string userId)
        {
            try
            {
                var response = await _service.GetTopTracksAsync(userId);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return HandleException(ex);
            }
        }

        [HttpGet("{userId}/top/albums")]
        public async Task<IActionResult> GetTopAlbumsAsync(string userId)
        {
            try
            {
                var response = await _service.GetTopAlbumsAsync(userId);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return HandleException(ex);
            }
        }

        [HttpGet("{userId}/top/artists")]
        public async Task<IActionResult> GetTopArtistsAsync(string userId)
        {
            try
            {
                var response = await _service.GetTopArtistsAsync(userId);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return HandleException(ex);
            }
        }

        [HttpGet("{id}/recently-played")]
        public async Task<IActionResult> GetRecentlyPlayed(string id)
        {
            try
            {
                var recentlyPlayed = await _service.GetRecentlyPlayedAsync(id);
                return Ok(recentlyPlayed);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("{userId}/favorites/tracks")]
        public IActionResult GetFavoriteTracks(string userId)
        {
            try
            {
                var tracks = _service.GetFavoriteTracks(userId);
                return Ok(tracks);
            }
            catch (Exception ex)
            {
                return HandleException(ex);
            }
        }

        [HttpPatch("{userId}/favorites/tracks/{trackId}")]
        public IActionResult PatchFavoriteTrack(string userId, string trackId)
        {
            try
            {
                var track = _service.PatchFavoriteTrack(userId, trackId);
                return Ok(track);
            }
            catch (Exception ex)
            {
                return HandleException(ex);
            }
        }

        [HttpGet("{userId}/favorites/albums")]
        public IActionResult GetFavoriteAlbums(string userId)
        {
            try
            {
                var albums = _service.GetFavoriteAlbums(userId);
                return Ok(albums);
            }
            catch (Exception ex)
            {
                return HandleException(ex);
            }
        }

        [HttpPatch("{userId}/favorites/albums/{albumId}")]
        public IActionResult PatchFavoriteAlbum(string userId, string albumId)
        {
            try
            {
                var album = _service.PatchFavoriteAlbum(userId, albumId);
                return Ok(album);
            }
            catch (Exception ex)
            {
                return HandleException(ex);
            }
        }

        [HttpGet("{userId}/favorites/artists")]
        public IActionResult GetFavoriteArtists(string userId)
        {
            try
            {
                var artists = _service.GetFavoriteArtists(userId);
                return Ok(artists);
            }
            catch (Exception ex)
            {
                return HandleException(ex);
            }
        }

        [HttpPatch("{userId}/favorites/artists/{artistId}")]
        public IActionResult PatchFavoriteArtist(string userId, string artistId)
        {
            try
            {
                var artist = _service.PatchFavoriteArtist(userId, artistId);
                return Ok(artist);
            }
            catch (Exception ex)
            {
                return HandleException(ex);
            }
        }
    }
}
