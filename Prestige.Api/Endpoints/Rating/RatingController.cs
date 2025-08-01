using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Prestige.Api.Endpoints.Rating.RequestResponse;

namespace Prestige.Api.Endpoints.Rating
{
    [Authorize]
    [Route("api/[controller]")]
    public class RatingController : BaseApiController
    {
        private readonly RatingServices _ratingServices;
        private readonly ILogger<RatingController> _logger;

        public RatingController(RatingServices ratingServices, ILogger<RatingController> logger)
        {
            _ratingServices = ratingServices;
            _logger = logger;
        }

        [HttpGet("categories")]
        public async Task<IActionResult> GetRatingCategories()
        {
            _logger.LogInformation("Getting rating categories");
            var categories = await _ratingServices.GetRatingCategoriesAsync();
            return Ok(categories);
        }

        [HttpPost("rate/{itemType}/{itemId}")]
        public async Task<IActionResult> StartRating(string itemType, string itemId)
        {
            _logger.LogInformation("Starting rating process for {ItemType} {ItemId}", itemType, itemId);
            return Ok();
        }

        [HttpPost("compare")]
        public async Task<IActionResult> SubmitComparison([FromBody] ComparisonRequest request)
        {
            _logger.LogInformation("Submitting comparison for {ItemType}", request.ItemType);
            return Ok();
        }

        [HttpGet("user/{itemType}")]
        public async Task<IActionResult> GetUserRatings(string itemType)
        {
            _logger.LogInformation("Getting user ratings for {ItemType}", itemType);
            return Ok();
        }

        [HttpGet("suggestions")]
        public async Task<IActionResult> GetRatingSuggestions()
        {
            _logger.LogInformation("Getting rating suggestions");
            return Ok();
        }
    }
}