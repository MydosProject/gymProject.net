using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NO23.Web.Domain.Entities;

namespace NO23.Web.Data.Configurations;

public class KitchenProductionPlanConfiguration : IEntityTypeConfiguration<KitchenProductionPlan>
{
    public void Configure(EntityTypeBuilder<KitchenProductionPlan> builder)
    {
        builder.Property(plan => plan.Status)
            .HasConversion<string>()
            .HasMaxLength(40);

        builder.HasIndex(plan => plan.PlanDate)
            .IsUnique();

        builder.Property(plan => plan.CreatedAtUtc)
            .HasDefaultValueSql("NOW()");
    }
}
