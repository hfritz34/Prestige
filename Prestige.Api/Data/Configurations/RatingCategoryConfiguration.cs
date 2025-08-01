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

            // Seed initial categories
            builder.HasData(
                new { Id = 1, Name = "Loved", MinScore = 75.0m, MaxScore = 100.0m, ColorHex = "#22c55e", DisplayOrder = 1 },
                new { Id = 2, Name = "Liked", MinScore = 50.0m, MaxScore = 74.99m, ColorHex = "#84cc16", DisplayOrder = 2 },
                new { Id = 3, Name = "Okay", MinScore = 25.0m, MaxScore = 49.99m, ColorHex = "#eab308", DisplayOrder = 3 },
                new { Id = 4, Name = "Disliked", MinScore = 0.0m, MaxScore = 24.99m, ColorHex = "#ef4444", DisplayOrder = 4 }
            );
        }
    }
}