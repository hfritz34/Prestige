namespace Prestige.Api.Endpoints.Library.RequestResponse
{
    public class BatchUpdateRequest
    {
        public string BatchId { get; set; } = string.Empty;
        public List<RecentlyPlayedItem> Items { get; set; } = new();
    }

    public class RecentlyPlayedItem
    {
        public string UserId { get; set; } = string.Empty;
        public string TrackId { get; set; } = string.Empty;
        public int Duration_ms { get; set; }
        public string Played_at { get; set; } = string.Empty;
    }
}