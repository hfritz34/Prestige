using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Prestige.Api.Domain;

namespace Prestige.Api.Data.Configurations
{
    public class UserArtistConfiguration : IEntityTypeConfiguration<UserArtist>
    {
        public void Configure(EntityTypeBuilder<UserArtist> builder)
        {
            builder.ToTable("UserArtist");

            builder.Property<string>("UserId").IsRequired();
            builder.Property<string>("ArtistId").IsRequired();

            builder.HasOne(ua => ua.Artist)
                   .WithMany()
                   .HasForeignKey("ArtistId");

            builder.HasKey("UserId", "ArtistId");

            // Index for ordering by TotalTime (descending for top artists)
            builder.HasIndex(nameof(UserArtist.TotalTime))
                   .HasDatabaseName("IX_UserArtist_TotalTime");
            
            // Composite index for user-specific queries ordered by TotalTime
            builder.HasIndex("UserId", nameof(UserArtist.TotalTime))
                   .HasDatabaseName("IX_UserArtist_UserId_TotalTime");
        }
    }
}
