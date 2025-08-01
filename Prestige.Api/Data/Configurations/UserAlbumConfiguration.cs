using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Prestige.Api.Domain;

namespace Prestige.Api.Data.Configurations
{
    public class UserAlbumConfiguration : IEntityTypeConfiguration<UserAlbum>
    {
        public void Configure(EntityTypeBuilder<UserAlbum> builder)
        {
            builder.ToTable("UserAlbum");

            builder.Property<string>("UserId").IsRequired();
            builder.Property<string>("AlbumId").IsRequired();
            builder.Property<int>("TotalTime").IsRequired();
            builder.Property<bool>("IsFavorite").IsRequired();
            builder.Property<decimal?>("PersonalRatingScore").HasPrecision(5, 2);
            builder.Property<int?>("RatingPosition");

            builder.HasOne(ua => ua.Album)
                   .WithMany()
                   .HasForeignKey("AlbumId");

            builder.HasKey("UserId", "AlbumId");

            // Index for ordering by TotalTime (descending for top albums)
            builder.HasIndex(nameof(UserAlbum.TotalTime))
                   .HasDatabaseName("IX_UserAlbum_TotalTime");
            
            // Composite index for user-specific queries ordered by TotalTime
            builder.HasIndex("UserId", nameof(UserAlbum.TotalTime))
                   .HasDatabaseName("IX_UserAlbum_UserId_TotalTime");
        }
    }
}
