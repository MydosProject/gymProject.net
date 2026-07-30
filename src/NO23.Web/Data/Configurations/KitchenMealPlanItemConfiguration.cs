using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NO23.Web.Domain.Entities;

namespace NO23.Web.Data.Configurations;

public class KitchenMealPlanItemConfiguration : IEntityTypeConfiguration<KitchenMealPlanItem>
{
    public void Configure(EntityTypeBuilder<KitchenMealPlanItem> builder)
    {
        builder.Property(item => item.MealSlot)
            .HasConversion<string>()
            .HasMaxLength(40);

        builder.Property(item => item.ProductNameSnapshot)
            .HasMaxLength(140)
            .IsRequired();

        builder.Property(item => item.ProteinGramsSnapshot)
            .HasPrecision(7, 2);

        builder.Property(item => item.CarbohydrateGramsSnapshot)
            .HasPrecision(7, 2);

        builder.Property(item => item.FatGramsSnapshot)
            .HasPrecision(7, 2);

        builder.Property(item => item.UnitPriceSnapshot)
            .HasPrecision(10, 2);

        builder.Property(item => item.IsSkipped)
            .HasDefaultValue(false);

        builder.HasIndex(item => new { item.KitchenMealPlanDayId, item.MealSlot })
            .IsUnique();

        builder.HasOne(item => item.KitchenMealPlanDay)
            .WithMany(day => day.Items)
            .HasForeignKey(item => item.KitchenMealPlanDayId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(item => item.KitchenMenuItem)
            .WithMany(menuItem => menuItem.MealPlanItems)
            .HasForeignKey(item => item.KitchenMenuItemId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
