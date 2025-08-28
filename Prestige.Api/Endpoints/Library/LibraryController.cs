using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
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

        [HttpPost("items/batch")]
        public async Task<IActionResult> GetItemDetailsBatch([FromBody] BatchItemRequest request)
        {
            try
            {
                _logger.LogInformation("Getting batch item details for {Count} items", request.Items.Count);
                var results = await _libraryServices.GetItemDetailsBatchAsync(request.Items);
                return Ok(new BatchItemResponse { Items = results });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting batch item details");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("{userId}/recently-updated")]
        [Authorize]
        public async Task<IActionResult> GetRecentlyUpdated(string userId, [FromQuery] string? since = null)
        {
            try
            {
                DateTime sinceDateTime;
                if (!string.IsNullOrEmpty(since))
                {
                    if (!DateTime.TryParse(since, out sinceDateTime))
                    {
                        _logger.LogWarning("Invalid 'since' parameter provided: {Since}", since);
                        return BadRequest("Invalid 'since' parameter format");
                    }
                }
                else
                {
                    // Default to last hour
                    sinceDateTime = DateTime.UtcNow.AddHours(-1);
                }

                _logger.LogInformation("Getting recently updated items for user {UserId} since {Since}", userId, sinceDateTime);
                var result = await _libraryServices.GetRecentlyUpdatedAsync(userId, sinceDateTime);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting recently updated items for user {UserId}", userId);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPost("batch-update")]
        [AllowAnonymous]
        public async Task<IActionResult> ProcessRecentlyPlayedBatch([FromBody] BatchUpdateRequest request)
        {
            try
            {
                _logger.LogInformation("Processing recently played batch {BatchId} with {Count} items", 
                    request.BatchId, request.Items.Count);

                await _libraryServices.ProcessRecentlyPlayedBatchUnauthenticatedAsync(request);
                
                return Ok(new { message = "Batch processed successfully", batchId = request.BatchId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing recently played batch {BatchId}", request.BatchId);
                return StatusCode(500, "Internal server error");
            }
        }
    }
}