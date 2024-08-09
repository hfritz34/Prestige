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
        }
    }
}
