using Microsoft.AspNetCore.Mvc;
using Prestige.Api.Endpoints.Library.RequestResponse;

namespace Prestige.Api.Endpoints.Library
{
    [ApiController]
    [Route("api/library")]
    public class LibraryController : ControllerBase
    {
        private readonly LibraryServices _libraryServices;
        private readonly ILogger<LibraryController> _logger;

        public LibraryController(LibraryServices libraryServices, ILogger<LibraryController> logger)
        {
            _libraryServices = libraryServices;
            _logger = logger;
        }

        [HttpGet("item/{itemType}/{itemId}")]
        public async Task<IActionResult> GetItemDetails(string itemType, string itemId)
        {
            try
            {
                _logger.LogInformation("Getting item details for {ItemType} {ItemId}", itemType, itemId);
                var result = await _libraryServices.GetItemDetailsAsync(itemType, itemId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting item details for {ItemType} {ItemId}", itemType, itemId);
                return StatusCode(500, "Internal server error");
            }
        }
    }
}