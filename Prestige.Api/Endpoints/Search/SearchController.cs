using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Prestige.Api.Endpoints.Search
{
    [Authorize]
    [Route("api/search")]
    public class SearchController : Endpoints.BaseApiController
    {
        private readonly SearchServices _services;
        private readonly ILogger<SearchController> _logger;

        public SearchController(SearchServices services, ILogger<SearchController> logger)
        {
            _services = services;
            _logger = logger;
        }

        // GET api/search/user-library?query=...&type=track|album|artist|all&page=1&pageSize=20
        [HttpGet("user-library")]
        public async Task<IActionResult> SearchUserLibrary(
            [FromQuery] string query,
            [FromQuery] string type = "all",
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            if (string.IsNullOrWhiteSpace(query))
                return BadRequest("query is required");

            var normalizedType = (type ?? "all").ToLowerInvariant();
            if (normalizedType is not ("track" or "album" or "artist" or "all"))
                return BadRequest("type must be track|album|artist|all");

            _logger.LogInformation("Searching user library: query={Query}, type={Type}, page={Page}, size={PageSize}",
                query, normalizedType, page, pageSize);

            var result = await _services.SearchUserLibraryAsync(query, normalizedType, page, pageSize);
            return Ok(result);
        }
    }
}


