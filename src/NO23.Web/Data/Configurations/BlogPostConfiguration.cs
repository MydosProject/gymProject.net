using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NO23.Web.Domain.Entities;

namespace NO23.Web.Data.Configurations;

public class BlogPostConfiguration : IEntityTypeConfiguration<BlogPost>
{
    public void Configure(EntityTypeBuilder<BlogPost> builder)
    {
        builder.Property(item => item.Title)
            .HasMaxLength(180)
            .IsRequired();

        builder.Property(item => item.Slug)
            .HasMaxLength(180)
            .IsRequired();

        builder.HasIndex(item => item.Slug)
            .IsUnique();

        builder.Property(item => item.Summary)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(item => item.Content)
            .HasMaxLength(12000)
            .IsRequired();

        builder.Property(item => item.Category)
            .HasMaxLength(80)
            .IsRequired();

        builder.Property(item => item.Tags)
            .HasMaxLength(500);

        builder.Property(item => item.CoverImageUrl)
            .HasMaxLength(500);

        builder.Property(item => item.Status)
            .HasConversion<string>()
            .HasMaxLength(40);

        builder.Property(item => item.CreatedAtUtc)
            .HasDefaultValueSql("NOW()");
    }
}
