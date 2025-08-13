namespace Prestige.Api.Endpoints.Library.RequestResponse
{
    public class BatchItemRequest
    {
        public List<ItemRequest> Items { get; set; } = new List<ItemRequest>();
    }

    public class ItemRequest
    {
        public string Id { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty; // "track", "album", "artist"
    }
}