namespace Prestige.Api.Domain;

public sealed class CollectionEntry
{
    public string UserId { get; private set; } = "";
    public string ItemType { get; private set; } = "";
    public string ItemId { get; private set; } = "";
    public string? TrackId { get; private set; }
    public string? AlbumId { get; private set; }
    public string? ArtistId { get; private set; }
    public Track? Track { get; private set; }
    public Album? Album { get; private set; }
    public Artist? Artist { get; private set; }
    public bool IsFavorite { get; private set; }
    public bool IsPinned { get; private set; }
    public DateTime SavedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private CollectionEntry() { }

    public CollectionEntry(string userId, string itemType, string itemId)
    {
        UserId = userId;
        ItemType = itemType;
        ItemId = itemId;
        switch (itemType)
        {
            case "track": TrackId = itemId; break;
            case "album": AlbumId = itemId; break;
            case "artist": ArtistId = itemId; break;
            default: throw new ArgumentException("Unknown catalog item type.", nameof(itemType));
        }
        SavedAt = UpdatedAt = DateTime.UtcNow;
    }

    public void Update(bool isFavorite, bool isPinned)
    {
        if (IsFavorite == isFavorite && IsPinned == isPinned) return;
        IsFavorite = isFavorite;
        IsPinned = isPinned;
        UpdatedAt = DateTime.UtcNow;
    }
}
