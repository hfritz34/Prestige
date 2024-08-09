using Prestige.Api.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Prestige.Api.Data.Configurations
{
    public class ImageConfiguration : IEntityTypeConfiguration<Image>
    {
        public void Configure(EntityTypeBuilder<Image> builder)
        {
            builder.ToTable("Image");

            builder.HasKey(image => image.Url);

            builder.Property(image => image.Width)
                .IsRequired();

            builder.Property(image => image.Height)
                .IsRequired();

        }
    }
}