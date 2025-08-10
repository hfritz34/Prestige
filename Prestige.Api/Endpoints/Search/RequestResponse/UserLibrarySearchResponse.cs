using System.Collections.Generic;

namespace Prestige.Api.Endpoints.Search
{
    public class UserLibrarySearchItem
    {
        public string Id { get; set; } = default!;
        public string Name { get; set; } = default!;
        public string? ImageUrl { get; set; }
        public List<string> Artists { get; set; } = new();
        public string? AlbumName { get; set; }
        public string ItemType { get; set; } = default!; // track | album | artist
    }

    public class UserLibrarySearchResult
    {
        public int Total { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public List<UserLibrarySearchItem> Items { get; set; } = new();
    }
}


