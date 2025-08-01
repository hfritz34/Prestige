using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Prestige.Api.Domain;

namespace Prestige.Api.Data.Configurations
{
    public class UserTrackConfiguration : IEntityTypeConfiguration<UserTrack>
    {
        public void Configure(EntityTypeBuilder<UserTrack> builder)
        {
            builder.ToTable("UserTrack");

            builder.Property<string>("UserId").IsRequired();
            builder.Property<string>("TrackId").IsRequired();
            builder.Property<decimal?>("PersonalRatingScore").HasPrecision(5, 2);
            builder.Property<int?>("RatingPosition");

            builder.HasOne(ut => ut.Track)
                   .WithMany()
                   .HasForeignKey("TrackId");

            builder.HasKey("UserId", "TrackId");

            // Index for ordering by TotalTime (descending for top tracks)
            builder.HasIndex(nameof(UserTrack.TotalTime))
                   .HasDatabaseName("IX_UserTrack_TotalTime");
            
            // Composite index for user-specific queries ordered by TotalTime
            builder.HasIndex("UserId", nameof(UserTrack.TotalTime))
                   .HasDatabaseName("IX_UserTrack_UserId_TotalTime");
        }
    }
}
