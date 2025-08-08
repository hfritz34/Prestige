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
            try
            {
                _logger.LogInformation("Starting rating process for {ItemType} {ItemId}", itemType, itemId);
                var result = await _ratingServices.StartRatingAsync(itemType, itemId);
                return Ok(result);
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Error starting rating for {ItemType} {ItemId}", itemType, itemId);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPost("compare")]
        public async Task<IActionResult> SubmitComparison([FromBody] ComparisonRequest request)
        {
            try
            {
                _logger.LogInformation("Submitting comparison for {ItemType}", request.ItemType);
                var result = await _ratingServices.SubmitComparisonAsync(request);
                return Ok(result);
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Error submitting comparison for {ItemType}", request.ItemType);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("user/{itemType}")]
        public async Task<IActionResult> GetUserRatings(string itemType)
        {
            try
            {
                _logger.LogInformation("Getting user ratings for {ItemType}", itemType);
                var result = await _ratingServices.GetUserRatingsAsync(itemType);
                return Ok(result);
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Error getting user ratings for {ItemType}", itemType);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPost("save")]
        public async Task<IActionResult> SaveRating([FromBody] SaveRatingRequest request)
        {
            try
            {
                _logger.LogInformation("Saving rating for {ItemType} {ItemId} with score {Score}", 
                    request.ItemType, request.ItemId, request.PersonalScore);
                var result = await _ratingServices.SaveRatingAsync(request.ItemType, request.ItemId, request.PersonalScore, request.CategoryId);
                return Ok(result);
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Error saving rating for {ItemType} {ItemId}", request.ItemType, request.ItemId);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpDelete("user/{itemType}/{itemId}")]
        public async Task<IActionResult> DeleteRating(string itemType, string itemId)
        {
            try
            {
                _logger.LogInformation("Deleting rating for {ItemType} {ItemId}", itemType, itemId);
                var result = await _ratingServices.DeleteRatingAsync(itemType, itemId);
                return Ok(result);
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Error deleting rating for {ItemType} {ItemId}", itemType, itemId);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("suggestions")]
        public async Task<IActionResult> GetRatingSuggestions()
        {
            _logger.LogInformation("Getting rating suggestions");
            return Ok();
        }
    }
}