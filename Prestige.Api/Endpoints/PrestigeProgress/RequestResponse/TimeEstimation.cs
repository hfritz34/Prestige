using System.Text.Json.Serialization;

namespace Prestige.Api.Endpoints.PrestigeProgress.RequestResponse
{
    public class TimeEstimation
    {
        [JsonPropertyName("minutes_remaining")]
        public double MinutesRemaining { get; set; }

        [JsonPropertyName("formatted_time")]
        public string FormattedTime { get; set; } = string.Empty;

        [JsonPropertyName("estimation_type")]
        public string EstimationType { get; set; } = string.Empty;
    }
}