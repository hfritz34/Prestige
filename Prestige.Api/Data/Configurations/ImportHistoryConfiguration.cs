using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Prestige.Api.Domain;

namespace Prestige.Api.Data.Configurations
{
    public class ImportHistoryConfiguration : IEntityTypeConfiguration<ImportHistory>
    {
        public void Configure(EntityTypeBuilder<ImportHistory> builder)
        {
            builder.HasKey(ih => ih.Id);
            
            builder.Property(ih => ih.UserId)
                .IsRequired()
                .HasMaxLength(255);
                
            builder.Property(ih => ih.FileHash)
                .IsRequired()
                .HasMaxLength(64); // SHA-256 hash length
                
            builder.Property(ih => ih.FileName)
                .IsRequired()
                .HasMaxLength(255);
                
            builder.Property(ih => ih.BatchId)
                .IsRequired()
                .HasMaxLength(50);
                
            builder.Property(ih => ih.Status)
                .IsRequired()
                .HasMaxLength(500);
                
            builder.HasIndex(ih => new { ih.UserId, ih.FileHash })
                .HasDatabaseName("IX_ImportHistory_UserId_FileHash");
                
            builder.HasIndex(ih => ih.BatchId)
                .HasDatabaseName("IX_ImportHistory_BatchId");
        }
    }
}