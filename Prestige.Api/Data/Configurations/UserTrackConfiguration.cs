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

            builder.HasOne(ut => ut.Track)
                   .WithMany()
                   .HasForeignKey("TrackId");


            builder.HasKey("UserId", "TrackId");

        }
    }
}
