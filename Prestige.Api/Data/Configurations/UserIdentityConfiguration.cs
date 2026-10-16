using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Prestige.Api.Domain;

namespace Prestige.Api.Data.Configurations;

public sealed class UserIdentityConfiguration : IEntityTypeConfiguration<UserIdentity>
{
    public void Configure(EntityTypeBuilder<UserIdentity> builder)
    {
        builder.ToTable("UserIdentity");
        builder.HasKey(identity => new { identity.Issuer, identity.Subject });
        builder.Property(identity => identity.Issuer).HasMaxLength(200);
        builder.Property(identity => identity.Subject).HasMaxLength(255);
        builder.HasOne(identity => identity.User).WithMany()
            .HasForeignKey(identity => identity.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}
