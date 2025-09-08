using System.Text.Json.Serialization;

namespace Prestige.Api.Endpoints.PrestigeProgress.RequestResponse
{
    public class ProgressStats
    {
        [JsonPropertyName("current_value")]
        public double CurrentValue { get; set; }

        [JsonPropertyName("next_threshold")]
        public double? NextThreshold { get; set; }

        [JsonPropertyName("percentage")]
        public double Percentage { get; set; }

        [JsonPropertyName("is_max_level")]
        public bool IsMaxLevel { get; set; }
    }
}