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
        public IActionResult PostUserTrack(string userId, [FromBody] UserTrackRequest request)
        {
            try
            {
                var track = _service.PostUserTrack(userId, request);
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
    }
}