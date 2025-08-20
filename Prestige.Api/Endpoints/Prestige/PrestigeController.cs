using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Prestige.Api.Endpoints.Prestige.RequestResponse;
using Exception = Prestige.Api.Exceptions.Exception;

namespace Prestige.Api.Endpoints.Prestige
{
    [ApiController]
    [Authorize]
    [Route("/prestige")]
    public class PrestigeController : BaseApiController
    {
        private readonly PrestigeServices _service;
        public PrestigeController(PrestigeServices service)
        {
            _service = service;
        }

        [HttpPost("{userId}/tracks")]
        public async Task<IActionResult> PostUserTrack(string userId, [FromBody] UserTrackRequest request)
        {
            try
            {
                var track = await _service.PostUserTrack(userId, request);
                return Ok(track);
            }
            catch (Exception ex)
            {
                return HandleException(ex);
            }
        }

        [HttpGet("{userId}/tracks/{trackId}")]
        public IActionResult GetUserTrack(string userId, string trackId)
        {
            try
            {
                var track = _service.GetUserTrack(userId, trackId);
                return Ok(track);
            }
            catch (Exception ex)
            {
                return HandleException(ex);
            }
        }

        [HttpGet("{userId}/albums/{albumId}")]
        public IActionResult GetUserAlbum(string userId, string albumId)
        {
            try
            {
                var album = _service.GetUserAlbum(userId, albumId);
                return Ok(album);
            }
            catch (Exception ex)
            {
                return HandleException(ex);
            }
        }

        [HttpGet("{userId}/artists/{artistId}")]
        public IActionResult GetUserArtist(string userId, string artistId)
        {
            try
            {
                var artist = _service.GetUserArtist(userId, artistId);
                return Ok(artist);
            }
            catch (Exception ex)
            {
                return HandleException(ex);
            }
        }

        [HttpGet("{userId}/albums/{albumId}/tracks")]
        public async Task<IActionResult> GetAlbumTracksWithRankings(string userId, string albumId)
        {
            try
            {
                var tracks = await _service.GetAlbumTracksWithRankings(userId, albumId);
                return Ok(tracks);
            }
            catch (Exception ex)
            {
                return HandleException(ex);
            }
        }

        [HttpPost("{userId}/tracks/{trackId}/pin")]
        public async Task<IActionResult> PinUserTrack(string userId, string trackId)
        {
            try
            {
                await _service.TogglePinUserTrack(userId, trackId);
                return Ok();
            }
            catch (Exception ex)
            {
                return HandleException(ex);
            }
        }

        [HttpPost("{userId}/albums/{albumId}/pin")]
        public async Task<IActionResult> PinUserAlbum(string userId, string albumId)
        {
            try
            {
                await _service.TogglePinUserAlbum(userId, albumId);
                return Ok();
            }
            catch (Exception ex)
            {
                return HandleException(ex);
            }
        }

        [HttpPost("{userId}/artists/{artistId}/pin")]
        public async Task<IActionResult> PinUserArtist(string userId, string artistId)
        {
            try
            {
                await _service.TogglePinUserArtist(userId, artistId);
                return Ok();
            }
            catch (Exception ex)
            {
                return HandleException(ex);
            }
        }

        [HttpGet("{userId}/pinned")]
        public IActionResult GetPinnedItems(string userId)
        {
            try
            {
                var items = _service.GetPinnedItems(userId);
                return Ok(items);
            }
            catch (Exception ex)
            {
                return HandleException(ex);
            }
        }
    }
}