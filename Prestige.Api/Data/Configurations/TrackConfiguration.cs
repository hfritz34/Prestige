using Prestige.Api.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Prestige.Api.Data.Configurations
{
    public class TrackConfiguration : IEntityTypeConfiguration<Track>
    {
        public void Configure(EntityTypeBuilder<Track> builder)
        {
            builder.ToTable("Track");

            builder.HasKey(track => track.Id);

            builder.Property(track => track.Name)
                .IsRequired()
                .HasMaxLength(255);

            builder.HasMany(track => track.Artists)
                .WithMany();

            builder.HasOne(track => track.Album)
                .WithMany();
        }
    }
}