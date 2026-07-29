using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NO23.Web.Domain.Entities;

namespace NO23.Web.Data.Configurations;

public class CommunityChallengeConfiguration : IEntityTypeConfiguration<CommunityChallenge>
{
    public void Configure(EntityTypeBuilder<CommunityChallenge> builder)
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

        builder.Property(item => item.Description)
            .HasMaxLength(4000)
            .IsRequired();

        builder.Property(item => item.Goal)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(item => item.Reward)
            .HasMaxLength(500);

        builder.Property(item => item.CalorieTolerancePercent)
            .HasPrecision(5, 2);

        builder.Property(item => item.Status)
            .HasConversion<string>()
            .HasMaxLength(40);

        builder.Property(item => item.ImageUrl)
            .HasMaxLength(500);

        builder.Property(item => item.CreatedAtUtc)
            .HasDefaultValueSql("NOW()");
    }
}
