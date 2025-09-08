using System.Text.Json.Serialization;

namespace Prestige.Api.Endpoints.PrestigeProgress.RequestResponse
{
    public class PrestigeTierInfo
    {
        [JsonPropertyName("tier")]
        public string Tier { get; set; } = string.Empty;

        [JsonPropertyName("display_name")]
        public string DisplayName { get; set; } = string.Empty;

        [JsonPropertyName("color")]
        public string Color { get; set; } = string.Empty;

        [JsonPropertyName("image_name")]
        public string ImageName { get; set; } = string.Empty;

        [JsonPropertyName("threshold")]
        public double Threshold { get; set; }
    }
}