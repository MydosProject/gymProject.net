using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NO23.Web.Domain.Entities;

namespace NO23.Web.Data.Configurations;

public class KitchenProductionPlanItemConfiguration : IEntityTypeConfiguration<KitchenProductionPlanItem>
{
    public void Configure(EntityTypeBuilder<KitchenProductionPlanItem> builder)
    {
        builder.Property(item => item.ProductNameSnapshot)
            .HasMaxLength(140)
            .IsRequired();

        builder.Property(item => item.Status)
            .HasConversion<string>()
            .HasMaxLength(40);

        builder.Property(item => item.CreatedAtUtc)
            .HasDefaultValueSql("NOW()");

        builder.HasIndex(item => new { item.KitchenProductionPlanId, item.KitchenMenuItemId })
            .IsUnique();

        builder.HasOne(item => item.KitchenProductionPlan)
            .WithMany(plan => plan.Items)
            .HasForeignKey(item => item.KitchenProductionPlanId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(item => item.KitchenMenuItem)
            .WithMany(menuItem => menuItem.ProductionPlanItems)
            .HasForeignKey(item => item.KitchenMenuItemId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
