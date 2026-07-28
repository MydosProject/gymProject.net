using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NO23.Web.Domain.Entities;

namespace NO23.Web.Data.Configurations;

public class KitchenMealPlanConfiguration : IEntityTypeConfiguration<KitchenMealPlan>
{
    public void Configure(EntityTypeBuilder<KitchenMealPlan> builder)
    {
        builder.Property(plan => plan.Status)
            .HasConversion<string>()
            .HasMaxLength(40);

        builder.Property(plan => plan.CalculationVersion)
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(plan => plan.SourceWeightKg)
            .HasPrecision(6, 2);

        builder.Property(plan => plan.SourceGender)
            .HasConversion<string>()
            .HasMaxLength(40);

        builder.Property(plan => plan.SourceActivityLevel)
            .HasConversion<string>()
            .HasMaxLength(40);

        builder.Property(plan => plan.SourceGoal)
            .HasConversion<string>()
            .HasMaxLength(40);

        builder.Property(plan => plan.GeneratedAtUtc)
            .HasDefaultValueSql("NOW()");

        builder.HasIndex(plan => plan.KitchenSubscriptionId)
            .IsUnique();

        builder.HasOne(plan => plan.KitchenSubscription)
            .WithOne(subscription => subscription.MealPlan)
            .HasForeignKey<KitchenMealPlan>(plan => plan.KitchenSubscriptionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
