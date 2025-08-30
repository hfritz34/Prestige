using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Text.Json;
using System.Text;

class Program
{
    private static readonly string ClientId = "1a335ea72e3c40a289d19be1d4817ff8";
    private static readonly string ClientSecret = "ca898b30d24a4c0a9c5b8557cba2385e";
    private static readonly string AlbumId = "075lV4wdtLwFvIvCUdSYhL";
    
    static async Task Main(string[] args)
    {
        try
        {
            var token = await GetSpotifyToken();
            var tracks = await GetAlbumTracks(token, AlbumId);
            
            Console.WriteLine("=== SPOTIFY TRACK DATA FOR FLEX MUSIX ===");
            Console.WriteLine();
            
            foreach (var track in tracks)
            {
                var trackName = track.GetProperty("name").GetString().ToLower();
                if (trackName.Contains("kills") || trackName.Contains("all star"))
                {
                    Console.WriteLine($"Track: {track.GetProperty("name").GetString()}");
                    Console.WriteLine($"ID: {track.GetProperty("id").GetString()}");
                    Console.WriteLine($"Duration: {track.GetProperty("duration_ms").GetInt32()}ms");
                    Console.WriteLine();
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }
    
    static async Task<string> GetSpotifyToken()
    {
        using var client = new HttpClient();
        
        var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{ClientId}:{ClientSecret}"));
        client.DefaultRequestHeaders.Add("Authorization", $"Basic {credentials}");
        
        var content = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("grant_type", "client_credentials")
        });
        
        var response = await client.PostAsync("https://accounts.spotify.com/api/token", content);
        var json = await response.Content.ReadAsStringAsync();
        var tokenData = JsonSerializer.Deserialize<JsonElement>(json);
        
        return tokenData.GetProperty("access_token").GetString();
    }
    
    static async Task<JsonElement[]> GetAlbumTracks(string token, string albumId)
    {
        using var client = new HttpClient();
        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
        
        var response = await client.GetAsync($"https://api.spotify.com/v1/albums/{albumId}/tracks");
        var json = await response.Content.ReadAsStringAsync();
        var data = JsonSerializer.Deserialize<JsonElement>(json);
        
        var items = data.GetProperty("items").EnumerateArray().ToArray();
        return items;
    }
}