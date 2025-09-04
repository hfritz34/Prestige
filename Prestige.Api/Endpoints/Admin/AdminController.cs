using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Prestige.Api.Configuration;

namespace Prestige.Api.Endpoints.Admin
{
    /// <summary>
    /// Admin endpoints for configuration management and debugging
    /// </summary>
    [ApiController]
    [Route("api/admin")]
    [Authorize] // You may want to add additional admin-specific authorization here
    public class AdminController : ControllerBase
    {
        /// <summary>
        /// Get current prestige configuration information
        /// Useful for debugging and verifying threshold settings
        /// </summary>
        /// <returns>Current prestige configuration details</returns>
        [HttpGet("prestige-config")]
        public IActionResult GetPrestigeConfiguration()
        {
            try
            {
                var config = PrestigeThresholds.GetConfigurationInfo();
                return Ok(config);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { 
                    message = "Error retrieving prestige configuration", 
                    error = ex.Message 
                });
            }
        }

        /// <summary>
        /// Test prestige tier calculation with custom values
        /// Useful for testing different listening times and threshold modes
        /// </summary>
        /// <param name="totalTimeMinutes">Total listening time in minutes</param>
        /// <param name="itemType">Item type: "track", "album", or "artist"</param>
        /// <returns>Calculated prestige tier and configuration details</returns>
        [HttpGet("test-prestige")]
        public IActionResult TestPrestigeTierCalculation([FromQuery] int totalTimeMinutes, [FromQuery] string itemType)
        {
            try
            {
                if (string.IsNullOrEmpty(itemType))
                {
                    return BadRequest("itemType is required (track, album, or artist)");
                }

                // Convert minutes to seconds for the calculation
                int totalTimeSeconds = totalTimeMinutes * 60;
                string prestigeTier = PrestigeThresholds.CalculatePrestigeTier(totalTimeSeconds, itemType);

                var result = new
                {
                    InputTimeMinutes = totalTimeMinutes,
                    InputTimeSeconds = totalTimeSeconds,
                    ItemType = itemType,
                    CalculatedTier = prestigeTier,
                    UsingDevThresholds = PrestigeThresholds.UseDevThresholds,
                    CurrentThresholds = PrestigeThresholds.GetThresholds(itemType),
                    AllTiers = PrestigeThresholds.TierNames
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { 
                    message = "Error calculating prestige tier", 
                    error = ex.Message 
                });
            }
        }

        /// <summary>
        /// Get environment variables related to prestige configuration
        /// </summary>
        /// <returns>Environment configuration details</returns>
        [HttpGet("environment")]
        public IActionResult GetEnvironmentInfo()
        {
            try
            {
                var envInfo = new
                {
                    UseDevPrestigeThresholds = Environment.GetEnvironmentVariable("USE_DEV_PRESTIGE_THRESHOLDS") ?? "not set",
                    AspNetCoreEnvironment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "not set",
                    CurrentlyUsingDevThresholds = PrestigeThresholds.UseDevThresholds,
                    ServerTime = DateTime.UtcNow,
                    Instructions = new
                    {
                        EnableDevThresholds = "Set environment variable: USE_DEV_PRESTIGE_THRESHOLDS=true",
                        EnableProdThresholds = "Set environment variable: USE_DEV_PRESTIGE_THRESHOLDS=false",
                        Note = "Changes require application restart to take effect"
                    }
                };

                return Ok(envInfo);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { 
                    message = "Error retrieving environment information", 
                    error = ex.Message 
                });
            }
        }

        /// <summary>
        /// Compare all tier thresholds between dev and production modes
        /// </summary>
        /// <returns>Detailed threshold comparison</returns>
        [HttpGet("threshold-comparison")]
        public IActionResult GetThresholdComparison()
        {
            try
            {
                var comparison = new
                {
                    CurrentMode = PrestigeThresholds.UseDevThresholds ? "Development" : "Production",
                    TierNames = PrestigeThresholds.TierNames,
                    
                    DevThresholds = new
                    {
                        Track = PrestigeThresholds.DevThresholds["track"],
                        Album = PrestigeThresholds.DevThresholds["album"],
                        Artist = PrestigeThresholds.DevThresholds["artist"]
                    },
                    
                    ProductionThresholds = new
                    {
                        Track = PrestigeThresholds.ProductionThresholds["track"],
                        Album = PrestigeThresholds.ProductionThresholds["album"],
                        Artist = PrestigeThresholds.ProductionThresholds["artist"]
                    },
                    
                    CurrentActiveThresholds = new
                    {
                        Track = PrestigeThresholds.GetThresholds("track"),
                        Album = PrestigeThresholds.GetThresholds("album"),
                        Artist = PrestigeThresholds.GetThresholds("artist")
                    }
                };

                return Ok(comparison);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { 
                    message = "Error retrieving threshold comparison", 
                    error = ex.Message 
                });
            }
        }
    }
}