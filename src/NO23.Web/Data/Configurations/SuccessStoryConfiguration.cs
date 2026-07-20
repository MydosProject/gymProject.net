using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NO23.Web.Domain.Entities;

namespace NO23.Web.Data.Configurations;

public class SuccessStoryConfiguration : IEntityTypeConfiguration<SuccessStory>
{
    public void Configure(EntityTypeBuilder<SuccessStory> builder)
    {
        builder.Property(item => item.MemberName)
            .HasMaxLength(140)
            .IsRequired();

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

        builder.Property(item => item.Story)
            .HasMaxLength(8000)
            .IsRequired();

        builder.Property(item => item.AchievementMetric)
            .HasMaxLength(160);

        builder.Property(item => item.BeforeImageUrl)
            .HasMaxLength(500);

        builder.Property(item => item.AfterImageUrl)
            .HasMaxLength(500);

        builder.Property(item => item.VideoUrl)
            .HasMaxLength(500);

        builder.Property(item => item.Status)
            .HasConversion<string>()
            .HasMaxLength(40);

        builder.Property(item => item.CreatedAtUtc)
            .HasDefaultValueSql("NOW()");
    }
}
