namespace Prestige.Api.Endpoints.Library.RequestResponse
{
    public class ItemDetailsResponse
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string ItemType { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public List<string>? Artists { get; set; }
        public string? AlbumName { get; set; }
    }
}