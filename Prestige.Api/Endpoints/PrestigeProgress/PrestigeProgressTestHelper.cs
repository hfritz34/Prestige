using Prestige.Api.Configuration;
using Prestige.Api.Endpoints.PrestigeProgress.RequestResponse;

namespace Prestige.Api.Endpoints.PrestigeProgress
{
    /// <summary>
    /// Helper class for testing and debugging prestige progress calculations
    /// Useful for verifying dev vs production threshold behavior
    /// </summary>
    public static class PrestigeProgressTestHelper
    {
        /// <summary>
        /// Test progress calculation across different listening times
        /// Shows how progress changes with dev vs production thresholds
        /// </summary>
        public static void TestProgressCalculations(string itemType = "track")
        {
            Console.WriteLine($"\n=== PRESTIGE PROGRESS TEST - {itemType.ToUpper()} ===");
            Console.WriteLine($"Dev Mode: {PrestigeThresholds.UseDevThresholds}");
            
            var thresholds = PrestigeThresholds.GetThresholds(itemType);
            var tierNames = PrestigeThresholds.TierNames;
            
            Console.WriteLine($"Thresholds: [{string.Join(", ", thresholds)}]");
            Console.WriteLine($"Total Tiers: {tierNames.Length}\n");

            // Test different listening times
            double[] testMinutes = PrestigeThresholds.UseDevThresholds 
                ? new double[] { 0, 0.5, 1, 2, 5, 10, 25, 50, 75, 100, 150 } // Dev test values
                : new double[] { 0, 5, 10, 25, 50, 100, 200, 500, 1000, 1500, 2500 }; // Prod test values

            foreach (var minutes in testMinutes)
            {
                var result = SimulateProgressCalculation(minutes, thresholds, tierNames);
                Console.WriteLine($"{minutes,6:F1}min → {result.CurrentTier} ({result.ProgressPercentage,5:F1}%) → {result.NextTier ?? "MAX"}");
            }
        }

        /// <summary>
        /// Simulate the progress calculation logic
        /// </summary>
        private static (string CurrentTier, double ProgressPercentage, string? NextTier) SimulateProgressCalculation(
            double currentMinutes, int[] thresholds, string[] tierNames)
        {
            // Find current tier index
            int tierIndex = 0;
            for (int i = 0; i < thresholds.Length; i++)
            {
                if (currentMinutes >= thresholds[i])
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
            
            if (!isMaxLevel)
            {
                if (tierIndex == 0) // None tier
                {
                    if (thresholds.Length > 0)
                    {
                        progressPercentage = Math.Min((currentMinutes / thresholds[0]) * 100.0, 100.0);
                    }
                }
                else // Between tiers
                {
                    var currentThreshold = thresholds[tierIndex - 1];
                    if (tierIndex < thresholds.Length)
                    {
                        var nextThreshold = thresholds[tierIndex];
                        var progressInTier = currentMinutes - currentThreshold;
                        var tierRange = nextThreshold - currentThreshold;
                        progressPercentage = tierRange > 0 ? Math.Min((progressInTier / tierRange) * 100.0, 100.0) : 0.0;
                    }
                }
            }

            return (currentTier, progressPercentage, nextTier);
        }

        /// <summary>
        /// Show threshold comparison between dev and production
        /// </summary>
        public static void CompareThresholds()
        {
            Console.WriteLine("\n=== DEV vs PRODUCTION THRESHOLD COMPARISON ===");
            
            var devTrack = PrestigeThresholds.DevThresholds["track"];
            var prodTrack = PrestigeThresholds.ProductionThresholds["track"];
            var tierNames = PrestigeThresholds.TierNames;

            Console.WriteLine("Tier Name        Dev Minutes    Prod Minutes   Ratio");
            Console.WriteLine("─────────────────────────────────────────────────────");
            
            for (int i = 0; i < Math.Min(devTrack.Length, prodTrack.Length); i++)
            {
                var tierName = tierNames[i + 1]; // +1 because index 0 is "None"
                var ratio = (double)prodTrack[i] / devTrack[i];
                Console.WriteLine($"{tierName,-15} {devTrack[i],10} {prodTrack[i],13} {ratio,7:F1}x");
            }

            Console.WriteLine($"\nDev mode allows reaching Prestige tier in {devTrack[devTrack.Length - 1]} minutes");
            Console.WriteLine($"Production mode requires {prodTrack[prodTrack.Length - 1]} minutes for Prestige tier");
            Console.WriteLine($"That's a {(double)prodTrack[prodTrack.Length - 1] / devTrack[devTrack.Length - 1]:F1}x difference!");
        }

        /// <summary>
        /// Validate that progress calculations are working correctly
        /// </summary>
        public static bool ValidateProgressCalculations()
        {
            Console.WriteLine("\n=== PROGRESS CALCULATION VALIDATION ===");
            
            var errors = new List<string>();
            
            // Test with both dev and production thresholds
            foreach (var itemType in new[] { "track", "album", "artist" })
            {
                var thresholds = PrestigeThresholds.GetThresholds(itemType);
                var tierNames = PrestigeThresholds.TierNames;
                
                // Test key boundary conditions
                var testCases = new[]
                {
                    (minutes: 0.0, expectedTier: "None"),
                    (minutes: (double)thresholds[0], expectedTier: tierNames[1]), // First tier
                    (minutes: (double)thresholds[0] - 0.1, expectedTier: "None"), // Just below first tier
                    (minutes: (double)thresholds[thresholds.Length - 1], expectedTier: "Prestige"), // Max tier
                };

                foreach (var (minutes, expectedTier) in testCases)
                {
                    var result = SimulateProgressCalculation(minutes, thresholds, tierNames);
                    if (result.CurrentTier != expectedTier)
                    {
                        errors.Add($"{itemType}: {minutes}min should be {expectedTier}, got {result.CurrentTier}");
                    }
                }
            }

            if (errors.Any())
            {
                Console.WriteLine("❌ Validation FAILED:");
                errors.ForEach(Console.WriteLine);
                return false;
            }
            else
            {
                Console.WriteLine("✅ All validations PASSED!");
                return true;
            }
        }
    }
}