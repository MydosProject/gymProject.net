using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NO23.Web.Domain.Entities;

namespace NO23.Web.Data.Configurations;

public class UploadedImageConfiguration : IEntityTypeConfiguration<UploadedImage>
{
    public void Configure(EntityTypeBuilder<UploadedImage> builder)
    {
        builder.HasKey(image => image.Id);

        builder.Property(image => image.Id)
            .ValueGeneratedNever();

        builder.Property(image => image.ContentType)
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(image => image.Data)
            .IsRequired();

        builder.Property(image => image.CreatedBy)
            .HasMaxLength(256);

        builder.HasIndex(image => image.CreatedAtUtc);
    }
}
