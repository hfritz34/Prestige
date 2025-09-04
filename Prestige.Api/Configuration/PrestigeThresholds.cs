using System.ComponentModel;

namespace Prestige.Api.Configuration
{
    /// <summary>
    /// Configuration class for prestige tier thresholds.
    /// Supports both development (low thresholds for testing) and production (realistic thresholds) modes.
    /// </summary>
    public static class PrestigeThresholds
    {
        /// <summary>
        /// Prestige tier names in order (matches web assets and iOS enum)
        /// Based on actual tier assets: bronze, silver, gold, sapphire, emerald, diamond, garnet, opal, peridot, jet, darkmatter
        /// </summary>
        public static readonly string[] TierNames = new string[]
        {
            "None",
            "Bronze", 
            "Silver",
            "Gold", 
            "Sapphire",
            "Emerald",
            "Diamond",
            "Garnet",
            "Opal", 
            "Peridot",
            "Jet",
            "Dark Matter"
        };

        /// <summary>
        /// Development thresholds (ultra-low for quick testing)
        /// Values in minutes - 11 thresholds for Bronze through Dark Matter
        /// </summary>
        public static readonly Dictionary<string, int[]> DevThresholds = new Dictionary<string, int[]>
        {
            ["track"] = new int[] { 4, 10, 16, 24, 36, 50, 70, 100, 150, 180, 240 },
            ["album"] = new int[] { 10, 20, 30, 40, 60, 80, 110, 140, 170, 200, 240 },
            ["artist"] = new int[] { 20, 30, 40, 60, 80, 100, 130, 160, 190, 220, 240 }
        };

        /// <summary>
        /// Production thresholds (realistic values for real users)
        /// Values in minutes - 11 thresholds for Bronze through Dark Matter
        /// </summary>
        public static readonly Dictionary<string, int[]> ProductionThresholds = new Dictionary<string, int[]>
        {
            ["track"] = new int[] { 60, 150, 300, 500, 800, 1200, 1600, 2200, 3000, 6000, 15000 },
            ["album"] = new int[] { 200, 350, 500, 1000, 2000, 4000, 6000, 10000, 15000, 30000, 50000 },
            ["artist"] = new int[] { 400, 750, 1200, 2000, 3000, 6000, 10000, 15000, 25000, 50000, 100000 }
        };

        /// <summary>
        /// Check if development thresholds are enabled via environment variable
        /// </summary>
        public static bool UseDevThresholds => 
            Environment.GetEnvironmentVariable("USE_DEV_PRESTIGE_THRESHOLDS")?.ToLower() == "true";

        /// <summary>
        /// Get thresholds for the specified item type based on current environment configuration
        /// </summary>
        /// <param name="itemType">Item type: "track", "album", or "artist"</param>
        /// <returns>Array of threshold values in minutes</returns>
        public static int[] GetThresholds(string itemType)
        {
            var normalizedType = itemType.ToLower();
            var thresholds = UseDevThresholds ? DevThresholds : ProductionThresholds;
            
            return thresholds.ContainsKey(normalizedType) ? thresholds[normalizedType] : new int[0];
        }

        /// <summary>
        /// Calculate prestige tier based on listening time
        /// </summary>
        /// <param name="totalTimeSeconds">Total listening time in seconds</param>
        /// <param name="itemType">Item type: "track", "album", or "artist"</param>
        /// <returns>Prestige tier name</returns>
        public static string CalculatePrestigeTier(int? totalTimeSeconds, string itemType)
        {
            if (!totalTimeSeconds.HasValue || totalTimeSeconds.Value == 0) return TierNames[0]; // "None"
            
            // Convert seconds to minutes for threshold comparison
            var timeInMinutes = Math.Floor((double)totalTimeSeconds.Value / 60.0);
            var thresholds = GetThresholds(itemType);
            
            if (thresholds.Length == 0) return TierNames[0]; // "None"
            
            // Find the highest tier the user has achieved
            int tierIndex = 0;
            for (int i = 0; i < thresholds.Length; i++)
            {
                if (timeInMinutes >= thresholds[i])
                {
                    tierIndex = i + 1; // +1 because index 0 is "None"
                }
                else
                {
                    break;
                }
            }
            
            // Ensure we don't exceed the available tiers
            tierIndex = Math.Min(tierIndex, TierNames.Length - 1);
            
            return TierNames[tierIndex];
        }

        /// <summary>
        /// Get current configuration info for debugging/admin purposes
        /// </summary>
        /// <returns>Configuration information</returns>
        public static object GetConfigurationInfo()
        {
            return new
            {
                DevThresholdsEnabled = UseDevThresholds,
                EnvironmentVariable = Environment.GetEnvironmentVariable("USE_DEV_PRESTIGE_THRESHOLDS") ?? "not set",
                CurrentThresholds = new
                {
                    Track = GetThresholds("track"),
                    Album = GetThresholds("album"),
                    Artist = GetThresholds("artist")
                },
                TierNames = TierNames,
                TierCount = TierNames.Length - 1, // Excluding "None"
                TierMapping = "None, Bronze, Silver, Gold, Sapphire, Emerald, Diamond, Garnet, Opal, Peridot, Jet, Dark Matter"
            };
        }
    }
}