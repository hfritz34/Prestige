using System.Text.Json.Serialization;

namespace Prestige.Api.Endpoints.PrestigeProgress.RequestResponse
{
    public class PrestigeProgressResponse
    {
        [JsonPropertyName("item_id")]
        public string ItemId { get; set; } = string.Empty;

        [JsonPropertyName("item_type")]
        public string ItemType { get; set; } = string.Empty;

        [JsonPropertyName("item_name")]
        public string ItemName { get; set; } = string.Empty;

        [JsonPropertyName("current_level")]
        public PrestigeTierInfo CurrentLevel { get; set; } = new();

        [JsonPropertyName("next_level")]
        public PrestigeTierInfo? NextLevel { get; set; }

        [JsonPropertyName("progress")]
        public ProgressStats Progress { get; set; } = new();

        [JsonPropertyName("estimated_time_to_next")]
        public TimeEstimation? EstimatedTimeToNext { get; set; }
    }
}