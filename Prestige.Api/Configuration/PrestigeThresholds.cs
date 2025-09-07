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
        /// New 16-tier structure: bronze -> silver -> gold -> emerald -> amber -> amethyst -> quartz -> diamond -> jade -> ruby -> pearl -> loveydovey -> tourmaline -> topaz -> tanazanite -> prestige
        /// </summary>
        public static readonly string[] TierNames = new string[]
        {
            "None",
            "Bronze", 
            "Silver",
            "Gold",
            "Emerald",
            "Amber", 
            "Amethyst",
            "Quartz",
            "Diamond",
            "Jade",
            "Ruby",
            "Pearl",
            "Loveydovey",
            "Tourmaline",
            "Topaz",
            "Tanazanite",
            "Prestige"
        };

        /// <summary>
        /// Development thresholds (scaled for week-long testing to reach Prestige tier)
        /// Values in minutes - 16 thresholds for Bronze through Prestige
        /// Designed for testers to reach Prestige with ~2-4 hours daily listening over 7 days
        /// </summary>
        public static readonly Dictionary<string, int[]> DevThresholds = new Dictionary<string, int[]>
        {
            ["track"] = new int[] { 1, 2, 4, 6, 9, 12, 16, 21, 27, 34, 42, 51, 61, 72, 84, 97 },
            ["album"] = new int[] { 2, 5, 9, 14, 20, 27, 35, 44, 54, 65, 77, 90, 104, 119, 135, 152 },
            ["artist"] = new int[] { 4, 8, 13, 19, 26, 34, 43, 53, 64, 76, 89, 103, 118, 134, 151, 169 }
        };

        /// <summary>
        /// Production thresholds (optimized for user engagement and progression)
        /// Values in minutes - 16 thresholds for Bronze through Prestige
        /// Easy entry (Bronze: 10min track) → Prestigious final tier (2+ years of dedicated listening)
        /// Exponential progression gets harder as tiers increase
        /// </summary>
        public static readonly Dictionary<string, int[]> ProductionThresholds = new Dictionary<string, int[]>
        {
            ["track"] = new int[] { 10, 20, 35, 55, 85, 125, 180, 250, 340, 460, 610, 800, 1050, 1350, 1750, 2250 },
            ["album"] = new int[] { 30, 60, 110, 180, 280, 420, 600, 850, 1200, 1650, 2250, 3000, 3950, 5150, 6650, 8500 },
            ["artist"] = new int[] { 60, 120, 200, 320, 480, 700, 1000, 1400, 1950, 2650, 3550, 4700, 6150, 7950, 10200, 13000 }
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
                TierMapping = "None, Bronze, Silver, Gold, Emerald, Amber, Amethyst, Quartz, Diamond, Jade, Ruby, Pearl, Loveydovey, Tourmaline, Topaz, Tanazanite, Prestige"
            };
        }
    }
}