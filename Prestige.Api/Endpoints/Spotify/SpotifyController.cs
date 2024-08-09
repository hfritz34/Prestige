using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Prestige.Api.Endpoints.Spotify.RequestResponse;
using Exception = Prestige.Api.Exceptions.Exception;

namespace Prestige.Api.Endpoints.Spotify
{

    [ApiController]
    [Authorize]
    [Route("/spotify")]
    public class SpotifyController : BaseApiController
    {
        private readonly SpotifyServices _service;
        public SpotifyController(SpotifyServices service)
        {
            _service = service;
        }

        [HttpGet("tracks/search")]
        public async Task<IActionResult> SearchForTracksAsync([FromQuery] SearchRequest request)
        {
            try
            {
                var response = await _service.SearchForTracksAsync(request);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return HandleException(ex);
            }
        }

        [HttpGet("albums/search")]
        public async Task<IActionResult> SearchForAlbumsAsync([FromQuery] SearchRequest request)
        {
            try
            {
                var response = await _service.SearchForAlbumsAsync(request);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return HandleException(ex);
            }
        }

        [HttpGet("artists/search")]
        public async Task<IActionResult> SearchForArtistsAsync([FromQuery] SearchRequest request)
        {
            try
            {
                var response = await _service.SearchForArtistsAsync(request);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return HandleException(ex);
            }
        }

        [HttpGet("tracks/{id}")]
        public async Task<IActionResult> GetTrackByIdAsync(string id)
        {
            try
            {
                var response = await _service.GetTrackByIdAsync(id);
                if (response == null)
                {
                    return NotFound();
                }
                return Ok(response);
            }
            catch (Exception ex)
            {
                return HandleException(ex);
            }
        }

        [HttpGet("tracks")]
        public async Task<IActionResult> GetTracksByIdsAsync([FromQuery] string ids)
        {
            try
            {
                var response = await _service.GetTracksByIdsAsync(ids);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return HandleException(ex);
            }
        }

        [HttpGet("albums/{id}")]
        public async Task<IActionResult> GetAlbumByIdAsync(string id)
        {
            try
            {
                var response = await _service.GetAlbumByIdAsync(id);
                if (response == null)
                {
                    return NotFound();
                }
                return Ok(response);
            }
            catch (Exception ex)
            {
                return HandleException(ex);
            }

        }

        [HttpGet("artists/{id}")]
        public async Task<IActionResult> GetArtistByIdAsync(string id)
        {
            try
            {
                var response = await _service.GetArtistByIdAsync(id);
                if (response == null)
                {
                    return NotFound();
                }
                return Ok(response);
            }
            catch (Exception ex)
            {
                return HandleException(ex);
            }
        }

    }
}