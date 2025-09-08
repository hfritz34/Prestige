using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Prestige.Api.Configuration;
using Prestige.Api.Endpoints.PrestigeProgress.RequestResponse;

namespace Prestige.Api.Endpoints.PrestigeProgress
{
    [ApiController]
    [Route("api/debug/prestige-progress")]
    [Authorize] // You may want to add admin-only authorization here
    public class PrestigeProgressDebugController : ControllerBase
    {
        /// <summary>
        /// Test prestige progress calculation with custom minutes
        /// Useful for debugging max level and edge cases
        /// </summary>
        [HttpGet("test")]
        public IActionResult TestProgressCalculation(
            [FromQuery] double minutes,
            [FromQuery] string itemType = "tracks")
        {
            try
            {
                var normalizedItemType = itemType.ToLower();
                if (!new[] { "tracks", "albums", "artists" }.Contains(normalizedItemType))
                {
                    return BadRequest("Invalid item type. Must be: tracks, albums, or artists");
                }

                var thresholds = PrestigeThresholds.GetThresholds(normalizedItemType);
                var tierNames = PrestigeThresholds.TierNames;

                // Simulate tier calculation
                int tierIndex = 0;
                for (int i = 0; i < thresholds.Length; i++)
                {
                    if (minutes >= thresholds[i])
                    {
                        tierIndex = i + 1;
                    }
                    else
                    {
                        break;
                    }
                }
                tierIndex = Math.Min(tierIndex, tierNames.Length - 1);

                var currentTier = tierNames[tierIndex];
                var isMaxLevel = tierIndex >= tierNames.Length - 1;
                var nextTier = isMaxLevel ? null : tierNames[tierIndex + 1];

                // Calculate progress percentage
                double progressPercentage = 100.0;
                double? nextThreshold = null;
                
                if (!isMaxLevel)
                {
                    if (tierIndex == 0) // None tier
                    {
                        nextThreshold = thresholds[0];
                        progressPercentage = Math.Min((minutes / thresholds[0]) * 100.0, 100.0);
                    }
                    else // Between tiers
                    {
                        var currentThreshold = thresholds[tierIndex - 1];
                        nextThreshold = thresholds[tierIndex];
                        var progressInTier = minutes - currentThreshold;
                        var tierRange = nextThreshold - currentThreshold;
                        progressPercentage = tierRange > 0 ? Math.Min((progressInTier / tierRange.Value) * 100.0, 100.0) : 0.0;
                    }
                }

                var result = new
                {
                    test_input = new
                    {
                        minutes = minutes,
                        item_type = itemType,
                        dev_mode = PrestigeThresholds.UseDevThresholds
                    },
                    thresholds = thresholds,
                    tier_names = tierNames,
                    calculation_result = new
                    {
                        current_tier = currentTier,
                        current_tier_index = tierIndex,
                        next_tier = nextTier,
                        is_max_level = isMaxLevel,
                        progress_percentage = Math.Round(progressPercentage, 2),
                        next_threshold = nextThreshold,
                        current_threshold = tierIndex > 0 ? thresholds[tierIndex - 1] : (double?)null
                    },
                    // Simulate what the actual API would return
                    simulated_api_response = new PrestigeProgressResponse
                    {
                        ItemId = "test-item-id",
                        ItemType = itemType,
                        ItemName = $"Test {itemType}",
                        CurrentLevel = new PrestigeTierInfo
                        {
                            Tier = currentTier.ToLower(),
                            DisplayName = currentTier,
                            Color = GetTierColor(currentTier),
                            ImageName = $"{currentTier.ToLower()}_prestige",
                            Threshold = tierIndex > 0 ? thresholds[tierIndex - 1] : 0
                        },
                        NextLevel = nextTier != null ? new PrestigeTierInfo
                        {
                            Tier = nextTier.ToLower(),
                            DisplayName = nextTier,
                            Color = GetTierColor(nextTier),
                            ImageName = $"{nextTier.ToLower()}_prestige",
                            Threshold = nextThreshold ?? 0
                        } : null,
                        Progress = new ProgressStats
                        {
                            CurrentValue = minutes,
                            NextThreshold = isMaxLevel ? null : nextThreshold,
                            Percentage = isMaxLevel ? 100.0 : progressPercentage,
                            IsMaxLevel = isMaxLevel
                        },
                        EstimatedTimeToNext = isMaxLevel ? null : new TimeEstimation
                        {
                            MinutesRemaining = nextThreshold.HasValue ? nextThreshold.Value - minutes : 0,
                            FormattedTime = "Test estimation",
                            EstimationType = "simulated"
                        }
                    }
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message, stackTrace = ex.StackTrace });
            }
        }

        /// <summary>
        /// Get configuration info for debugging
        /// </summary>
        [HttpGet("config")]
        public IActionResult GetConfiguration()
        {
            try
            {
                var config = PrestigeThresholds.GetConfigurationInfo();
                return Ok(config);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        private string GetTierColor(string tierName)
        {
            var colors = new Dictionary<string, string>
            {
                { "None", "#808080" },
                { "Bronze", "#CD7F32" },
                { "Silver", "#C0C0C0" },
                { "Gold", "#FFD700" },
                { "Emerald", "#50C878" },
                { "Amber", "#FFBF00" },
                { "Amethyst", "#9966CC" },
                { "Quartz", "#E8E8E8" },
                { "Diamond", "#B9F2FF" },
                { "Jade", "#00A86B" },
                { "Ruby", "#E0115F" },
                { "Pearl", "#F0EAD6" },
                { "Loveydovey", "#FF69B4" },
                { "Tourmaline", "#86608E" },
                { "Topaz", "#FFC87C" },
                { "Tanazanite", "#243B82" },
                { "Prestige", "#FF4500" }
            };

            return colors.GetValueOrDefault(tierName, "#888888");
        }
    }
}