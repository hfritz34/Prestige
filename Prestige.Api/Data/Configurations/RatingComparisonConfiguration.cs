using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Prestige.Api.Domain;

namespace Prestige.Api.Data.Configurations
{
    public class RatingComparisonConfiguration : IEntityTypeConfiguration<RatingComparison>
    {
        public void Configure(EntityTypeBuilder<RatingComparison> builder)
        {
            builder.HasKey(rc => rc.Id);

            builder.Property(rc => rc.ItemId1)
                .IsRequired()
                .HasMaxLength(50);

            builder.Property(rc => rc.ItemId2)
                .IsRequired()
                .HasMaxLength(50);

            builder.Property(rc => rc.ItemType)
                .IsRequired()
                .HasMaxLength(20);

            builder.Property(rc => rc.WinnerId)
                .IsRequired()
                .HasMaxLength(50);

            builder.HasOne(rc => rc.User)
                .WithMany()
                .HasForeignKey("UserId")
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex("UserId", "ItemType", "ComparisonDate");
            builder.HasIndex(rc => rc.ComparisonDate);
        }
    }
}