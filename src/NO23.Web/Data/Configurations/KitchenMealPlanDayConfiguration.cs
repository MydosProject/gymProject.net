using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NO23.Web.Domain.Entities;

namespace NO23.Web.Data.Configurations;

public class KitchenMealPlanDayConfiguration : IEntityTypeConfiguration<KitchenMealPlanDay>
{
    public void Configure(EntityTypeBuilder<KitchenMealPlanDay> builder)
    {
        builder.Property(day => day.TotalProteinGrams)
            .HasPrecision(7, 2);

        builder.Property(day => day.TotalCarbohydrateGrams)
            .HasPrecision(7, 2);

        builder.Property(day => day.TotalFatGrams)
            .HasPrecision(7, 2);

        builder.HasIndex(day => new { day.KitchenMealPlanId, day.DayNumber })
            .IsUnique();

        builder.HasOne(day => day.KitchenMealPlan)
            .WithMany(plan => plan.Days)
            .HasForeignKey(day => day.KitchenMealPlanId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
