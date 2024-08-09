using Prestige.Api.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Prestige.Api.Data.Configurations
{
    public class AlbumConfiguration : IEntityTypeConfiguration<Album>
    {
        public void Configure(EntityTypeBuilder<Album> builder)
        {
            builder.ToTable("Album");

            builder.HasKey(album => album.Id);

            builder.Property(album => album.Name)
                .IsRequired()
                .HasMaxLength(255);

            builder.Property(album => album.Name)
                .IsRequired()
                .HasMaxLength(255);
            // Add foreign key for album from album table in SQL database

            builder.HasMany(album => album.Artists)
                .WithMany();


            builder.HasMany(album => album.Images)
                .WithOne();
        }
    }
}