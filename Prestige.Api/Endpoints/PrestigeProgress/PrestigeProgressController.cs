using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Prestige.Api.Configuration;
using Prestige.Api.Endpoints.PrestigeProgress.RequestResponse;
using Exception = Prestige.Api.Exceptions.Exception;

namespace Prestige.Api.Endpoints.PrestigeProgress
{
    [ApiController]
    [Route("api")]
    [Authorize]
    public class PrestigeProgressController : BaseApiController
    {
        private readonly PrestigeProgressServices _prestigeProgressServices;

        public PrestigeProgressController(PrestigeProgressServices prestigeProgressServices)
        {
            _prestigeProgressServices = prestigeProgressServices;
        }

        /// <summary>
        /// Get prestige progress for the current user's item
        /// </summary>
        /// <param name="itemType">Type of item: tracks, albums, or artists</param>
        /// <param name="itemId">Spotify ID of the item</param>
        /// <returns>Prestige progress information</returns>
        [HttpGet("prestige-progress/{itemType}/{itemId}")]
        public async Task<IActionResult> GetPrestigeProgress(
            [FromRoute] string itemType,
            [FromRoute] string itemId)
        {
            try
            {
                Console.WriteLine($"🔵 PrestigeProgress: Starting request for itemType={itemType}, itemId={itemId}");
                
                // Validate item type
                if (!IsValidItemType(itemType))
                {
                    Console.WriteLine($"❌ PrestigeProgress: Invalid item type: {itemType}");
                    throw new Exception(40001, $"Invalid item type. Must be one of: tracks, albums, artists");
                }

                // Validate item ID
                if (string.IsNullOrWhiteSpace(itemId))
                {
                    Console.WriteLine($"❌ PrestigeProgress: Missing item ID");
                    throw new Exception(40002, "Item ID is required");
                }

                Console.WriteLine($"🔵 PrestigeProgress: Calling service for itemType={itemType}, itemId={itemId}");
                var progressResponse = await _prestigeProgressServices.GetPrestigeProgressAsync(itemType, itemId);
                Console.WriteLine($"✅ PrestigeProgress: Service returned successfully");
                return Ok(progressResponse);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ PrestigeProgress: Exception caught: {ex.Message}");
                Console.WriteLine($"❌ PrestigeProgress: Stack trace: {ex.StackTrace}");
                return HandleException(ex);
            }
        }

        /// <summary>
        /// Get prestige progress for a friend's item
        /// </summary>
        /// <param name="friendId">Friend's user ID</param>
        /// <param name="itemType">Type of item: tracks, albums, or artists</param>
        /// <param name="itemId">Spotify ID of the item</param>
        /// <returns>Friend's prestige progress information</returns>
        [HttpGet("friends/{friendId}/prestige-progress/{itemType}/{itemId}")]
        public async Task<IActionResult> GetFriendPrestigeProgress(
            [FromRoute] string friendId,
            [FromRoute] string itemType,
            [FromRoute] string itemId)
        {
            try
            {
                // Validate inputs
                if (string.IsNullOrWhiteSpace(friendId))
                {
                    throw new Exception(40003, "Friend ID is required");
                }

                if (!IsValidItemType(itemType))
                {
                    throw new Exception(40001, $"Invalid item type. Must be one of: tracks, albums, artists");
                }

                if (string.IsNullOrWhiteSpace(itemId))
                {
                    throw new Exception(40002, "Item ID is required");
                }

                var progressResponse = await _prestigeProgressServices.GetFriendPrestigeProgressAsync(friendId, itemType, itemId);
                return Ok(progressResponse);
            }
            catch (Exception ex)
            {
                return HandleException(ex);
            }
        }

        /// <summary>
        /// Validate that the item type is supported
        /// </summary>
        private bool IsValidItemType(string itemType)
        {
            var validTypes = new[] { "tracks", "albums", "artists" };
            return validTypes.Contains(itemType.ToLower());
        }
        
        /// <summary>
        /// Debug endpoint to check which thresholds are currently active
        /// </summary>
        [HttpGet("prestige-progress/debug/thresholds")]
        [AllowAnonymous] // Allow checking without auth for debugging
        public IActionResult GetActiveThresholds()
        {
            var configInfo = PrestigeThresholds.GetConfigurationInfo();
            Console.WriteLine($"🔍 DEBUG: Prestige thresholds configuration - {System.Text.Json.JsonSerializer.Serialize(configInfo)}");
            return Ok(configInfo);
        }
    }
}