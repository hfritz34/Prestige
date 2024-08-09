using Prestige.Api.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Prestige.Api.Data.Configurations
{
    public class ArtistConfiguration : IEntityTypeConfiguration<Artist>
    {
        public void Configure(EntityTypeBuilder<Artist> builder)
        {
            builder.ToTable("Artist");

            builder.HasKey(artist => artist.Id);

            builder.Property(artist => artist.Name)
                .IsRequired()
                .HasMaxLength(255);

            builder.HasMany(artist => artist.Images)
                .WithOne();
        }
    }
}