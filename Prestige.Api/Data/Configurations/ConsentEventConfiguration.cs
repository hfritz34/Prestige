using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Prestige.Api.Domain;

namespace Prestige.Api.Data.Configurations
{
    public class ConsentEventConfiguration : IEntityTypeConfiguration<ConsentEvent>
    {
        public void Configure(EntityTypeBuilder<ConsentEvent> builder)
        {
            builder.ToTable("ConsentEvents");

            builder.HasKey(history => history.Id);

            builder.Property(history => history.UserId)
                .HasMaxLength(255)
                .IsRequired();

            builder.Property(history => history.Timestamp)
                .HasColumnType("datetime2")
                .IsRequired();

            builder.Property(history => history.Field)
                .HasMaxLength(100)
                .IsRequired();

            builder.Property(history => history.TriggeredBy)
                .HasMaxLength(50)
                .IsRequired();

            builder.HasIndex(history => new { history.UserId, history.Timestamp })
                .HasDatabaseName("IX_ConsentEvents_UserId_Timestamp");
        }
    }
}
