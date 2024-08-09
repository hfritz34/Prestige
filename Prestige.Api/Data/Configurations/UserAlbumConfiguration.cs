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

            builder.HasOne(ua => ua.Album)
                   .WithMany()
                   .HasForeignKey("AlbumId");

            builder.HasKey("UserId", "AlbumId");
        }
    }
}
