using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Prestige.Api.Domain;

namespace Prestige.Api.Data.Configurations;

public sealed class CollectionEntryConfiguration : IEntityTypeConfiguration<CollectionEntry>
{
    public void Configure(EntityTypeBuilder<CollectionEntry> builder)
    {
        builder.ToTable("CollectionEntry", table => table.HasCheckConstraint("CK_CollectionEntry_Item", """
            ("ItemType" = 'track' AND "TrackId" IS NOT NULL AND "TrackId" = "ItemId" AND "AlbumId" IS NULL AND "ArtistId" IS NULL) OR
            ("ItemType" = 'album' AND "AlbumId" IS NOT NULL AND "AlbumId" = "ItemId" AND "TrackId" IS NULL AND "ArtistId" IS NULL) OR
            ("ItemType" = 'artist' AND "ArtistId" IS NOT NULL AND "ArtistId" = "ItemId" AND "TrackId" IS NULL AND "AlbumId" IS NULL)
            """));
        builder.HasKey(entry => new { entry.UserId, entry.ItemType, entry.ItemId });
        builder.Property(entry => entry.ItemType).HasMaxLength(6);
        builder.Property(entry => entry.ItemId).HasMaxLength(50);
        builder.HasOne<User>().WithMany().HasForeignKey(entry => entry.UserId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(entry => entry.Track).WithMany().HasForeignKey(entry => entry.TrackId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(entry => entry.Album).WithMany().HasForeignKey(entry => entry.AlbumId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(entry => entry.Artist).WithMany().HasForeignKey(entry => entry.ArtistId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(entry => new { entry.UserId, entry.UpdatedAt });
    }
}
