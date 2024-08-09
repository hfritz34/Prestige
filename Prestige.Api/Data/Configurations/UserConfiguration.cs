using Prestige.Api.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Prestige.Api.Data.Configurations
{
    public class UserConfiguration : IEntityTypeConfiguration<User>
    {
        public void Configure(EntityTypeBuilder<User> builder)
        {
            builder.ToTable("User");

            builder.HasKey(u => u.Id);

            builder.Property(u => u.Name)
                .IsRequired()
                .HasMaxLength(255);

            builder.Property(u => u.NickName)
                .IsRequired()
                .HasMaxLength(20);

            builder.Property(u => u.Email)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(u => u.ProfilePicURL);

            builder.Property(u => u.AccessToken)
                .IsRequired();

            builder.Property(u => u.RefreshToken)
                .IsRequired();

            builder.Property(u => u.ExpiresAt)
                .IsRequired()
                .HasColumnType("datetime2");

            builder.HasMany(u => u.UserTracks)
                .WithOne(ut => ut.User)
                .HasForeignKey("UserId");
            
            builder.HasMany(u => u.UserAlbums)
                .WithOne(ua => ua.User)
                .HasForeignKey("UserId");

            builder.HasMany(u => u.UserArtists)
                .WithOne(ua => ua.User)
                .HasForeignKey("UserId");

            builder.Property(u => u.IsSetup)
                .IsRequired();
        }
    }
}