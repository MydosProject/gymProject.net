using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NO23.Web.Domain.Entities;

namespace NO23.Web.Data.Configurations;

public class DiscountCampaignConfiguration : IEntityTypeConfiguration<DiscountCampaign>
{
    public void Configure(EntityTypeBuilder<DiscountCampaign> builder)
    {
        builder.Property(campaign => campaign.Code)
            .HasMaxLength(40)
            .IsRequired();

        builder.HasIndex(campaign => campaign.Code)
            .IsUnique();

        builder.Property(campaign => campaign.Name)
            .HasMaxLength(120)
            .IsRequired();

        builder.Property(campaign => campaign.MinimumSubtotal)
            .HasPrecision(10, 2);

        builder.Property(campaign => campaign.CreatedAtUtc)
            .HasDefaultValueSql("NOW()");

        builder.HasIndex(campaign => campaign.IsActive);
        builder.HasIndex(campaign => new { campaign.StartsAtUtc, campaign.EndsAtUtc });
    }
}
