using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Prestige.Api.Domain;

namespace Prestige.Api.Data.Configurations
{
    public class UserConsentConfiguration : IEntityTypeConfiguration<UserConsent>
    {
        public void Configure(EntityTypeBuilder<UserConsent> builder)
        {
            builder.ToTable("UserConsents");

            builder.HasKey(consent => consent.UserId);

            builder.Property(consent => consent.UserId)
                .HasMaxLength(255)
                .ValueGeneratedNever();

            builder.Property(consent => consent.AllowsPersonalInsights)
                .HasDefaultValue(true)
                .IsRequired();

            builder.Property(consent => consent.AllowsDerivedFeatureStorage)
                .HasDefaultValue(true)
                .IsRequired();

            builder.Property(consent => consent.AllowsAnonymizedAggregation)
                .HasDefaultValue(false)
                .IsRequired();

            builder.Property(consent => consent.AllowsModelTraining)
                .HasDefaultValue(false)
                .IsRequired();

            builder.Property(consent => consent.LastUpdatedAt)
                .HasColumnType("datetime2")
                .IsRequired();

            builder.Property(consent => consent.ConsentVersion)
                .HasMaxLength(50)
                .IsRequired();

            builder.HasMany(consent => consent.History)
                .WithOne()
                .HasForeignKey(history => history.UserId)
                .HasPrincipalKey(consent => consent.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
