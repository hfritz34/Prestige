
using System.Text.Json.Serialization;

namespace Prestige.Api.Endpoints.UserEndpoints.RequestResponse
{
    public class Auth0UserResponse
    {
        // [JsonPropertyName("country")]
        // public string Country { get; set; }

        // [JsonPropertyName("created_at")]
        // public DateTime CreatedAt { get; set; }

        [JsonPropertyName("email")]
        public required string Email { get; set; }

        // [JsonPropertyName("explicit_content")]
        // public Dictionary<string, bool> ExplicitContent { get; set; }

        // [JsonPropertyName("external_urls")]
        // public Dictionary<string, string> ExternalUrls { get; set; }

        // [JsonPropertyName("followers")]
        // public string Followers { get; set; }

        // [JsonPropertyName("href")]
        // public string Href { get; set; }

        [JsonPropertyName("identities")]
        public required ICollection<IdentitiesResponse> Identities { get; set; }

        // [JsonPropertyName("images")]
        // public Dictionary<string, string> Images { get; set; }

        [JsonPropertyName("name")]
        public required string Name { get; set; }

        [JsonPropertyName("nickname")]
        public required string NickName { get; set; }

        [JsonPropertyName("picture")]
        public required string Picture { get; set; }

        // [JsonPropertyName("product")]
        // public string Product { get; set; }

        // [JsonPropertyName("type")]
        // public string Type { get; set; }

        // [JsonPropertyName("updated_at")]
        // public DateTime UpdatedAt { get; set; }

        // [JsonPropertyName("uri")]
        // public string Uri { get; set; }

        [JsonPropertyName("user_id")]
        public required string UserId { get; set; }

        // [JsonPropertyName("last_ip")]
        // public string LastIp { get; set; }

        // [JsonPropertyName("last_login")]
        // public string LastLogin { get; set; }

        [JsonPropertyName("logins_count")]
        public int LoginsCount { get; set; }
    }

    
    public class IdentitiesResponse
    {
        [JsonPropertyName("provider")]
        public required string Provider { get; set; }
        [JsonPropertyName("access_token")]
        public required string AccessToken { get; set; }
        [JsonPropertyName("refresh_token")]
        public required string RefreshToken { get; set; }

        [JsonPropertyName("user_id")]
        public required string UserId { get; set; }
        [JsonPropertyName("connection")]
        public required string Connection { get; set; }
        [JsonPropertyName("isSocial")]
        public bool IsSocial { get; set; }
    }
}