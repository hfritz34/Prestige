using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Prestige.Api.Domain;

namespace Prestige.Api.Data.Configurations
{
    public class RatingCategoryConfiguration : IEntityTypeConfiguration<RatingCategory>
    {
        public void Configure(EntityTypeBuilder<RatingCategory> builder)
        {
            builder.HasKey(rc => rc.Id);

            builder.Property(rc => rc.Name)
                .IsRequired()
                .HasMaxLength(50);

            builder.Property(rc => rc.MinScore)
                .HasPrecision(5, 2);

            builder.Property(rc => rc.MaxScore)
                .HasPrecision(5, 2);

            builder.Property(rc => rc.ColorHex)
                .IsRequired()
                .HasMaxLength(7);

            builder.HasIndex(rc => rc.Name)
                .IsUnique();

            builder.HasIndex(rc => rc.DisplayOrder);

            // Seed initial categories with 10-point scale
            builder.HasData(
                new { Id = 1, Name = "Loved", MinScore = 6.8m, MaxScore = 10.0m, ColorHex = "#22c55e", DisplayOrder = 1 },
                new { Id = 2, Name = "Liked", MinScore = 3.4m, MaxScore = 6.7m, ColorHex = "#eab308", DisplayOrder = 2 },
                new { Id = 3, Name = "Disliked", MinScore = 0.0m, MaxScore = 3.3m, ColorHex = "#ef4444", DisplayOrder = 3 }
            );
        }
    }
}