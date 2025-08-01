using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Prestige.Api.Domain;

namespace Prestige.Api.Data.Configurations
{
    public class RatingConfiguration : IEntityTypeConfiguration<Rating>
    {
        public void Configure(EntityTypeBuilder<Rating> builder)
        {
            builder.HasKey(r => r.Id);

            builder.Property(r => r.ItemId)
                .IsRequired()
                .HasMaxLength(50);

            builder.Property(r => r.ItemType)
                .IsRequired()
                .HasMaxLength(20);

            builder.Property(r => r.PersonalScore)
                .HasPrecision(5, 2);

            builder.HasOne(r => r.User)
                .WithMany()
                .HasForeignKey("UserId")
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(r => r.Category)
                .WithMany()
                .HasForeignKey("CategoryId")
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(r => new { r.User, r.ItemId, r.ItemType })
                .IsUnique();

            builder.HasIndex(r => r.ItemType);
            builder.HasIndex(r => r.PersonalScore);
        }
    }
}