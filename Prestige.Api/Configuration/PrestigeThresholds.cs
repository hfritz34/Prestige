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
        /// Based on actual tier assets: bronze, silver, peridot, gold, emerald, sapphire, ruby, garnet, jade, amethyst, jet, diamond, opal, darkmatter, cosmic
        /// </summary>
        public static readonly string[] TierNames = new string[]
        {
            "None",
            "Bronze", 
            "Silver",
            "Peridot",
            "Gold",
            "Emerald", 
            "Sapphire",
            "Ruby",
            "Garnet",
            "Jade",
            "Amethyst",
            "Jet",
            "Diamond",
            "Opal", 
            "Dark Matter",
            "Cosmic"
        };

        /// <summary>
        /// Development thresholds (ultra-low for quick testing)
        /// Values in minutes - 15 thresholds for Bronze through Cosmic
        /// </summary>
        public static readonly Dictionary<string, int[]> DevThresholds = new Dictionary<string, int[]>
        {
            ["track"] = new int[] { 2, 5, 8, 12, 18, 25, 35, 50, 70, 95, 130, 170, 220, 300, 400 },
            ["album"] = new int[] { 5, 10, 15, 25, 35, 50, 70, 95, 130, 170, 220, 280, 350, 450, 600 },
            ["artist"] = new int[] { 10, 20, 30, 45, 65, 90, 120, 160, 210, 270, 340, 420, 520, 650, 800 }
        };

        /// <summary>
        /// Production thresholds (optimized for user engagement and progression)
        /// Values in minutes - 15 thresholds for Bronze through Cosmic
        /// Easy entry (Bronze: 10min track) → Prestigious Cosmic (2 years of 10min/day listening)
        /// Exponential progression gets harder as tiers increase
        /// </summary>
        public static readonly Dictionary<string, int[]> ProductionThresholds = new Dictionary<string, int[]>
        {
            ["track"] = new int[] { 10, 20, 35, 60, 100, 150, 220, 320, 450, 650, 900, 1300, 1800, 2400, 3000 },
            ["album"] = new int[] { 30, 60, 120, 200, 350, 550, 800, 1200, 1700, 2400, 3400, 4800, 6500, 10000, 12000 },
            ["artist"] = new int[] { 60, 120, 200, 350, 550, 850, 1300, 1900, 2700, 3700, 5000, 6500, 8500, 12000, 15000 }
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
                TierMapping = "None, Bronze, Silver, Peridot, Gold, Emerald, Sapphire, Ruby, Garnet, Jade, Amethyst, Jet, Diamond, Opal, Dark Matter, Cosmic"
            };
        }
    }
}