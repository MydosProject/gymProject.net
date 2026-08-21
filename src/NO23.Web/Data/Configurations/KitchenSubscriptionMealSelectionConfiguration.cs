using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NO23.Web.Domain.Entities;

namespace NO23.Web.Data.Configurations;

public class KitchenSubscriptionMealSelectionConfiguration
    : IEntityTypeConfiguration<KitchenSubscriptionMealSelection>
{
    public void Configure(
        EntityTypeBuilder<KitchenSubscriptionMealSelection> builder)
    {
        builder.HasKey(selection => selection.Id);

        builder.Property(selection => selection.MealSlot)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(selection => selection.DailyPriceSnapshot)
            .HasPrecision(10, 2)
            .IsRequired();

        builder.Property(selection => selection.CalorieRatioSnapshot)
            .HasPrecision(5, 4)
            .IsRequired();

        builder.Property(selection => selection.CreatedAtUtc)
            .IsRequired();

        builder.HasOne(selection => selection.KitchenSubscription)
            .WithMany(subscription => subscription.MealSelections)
            .HasForeignKey(selection => selection.KitchenSubscriptionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(selection => new
        {
            selection.KitchenSubscriptionId,
            selection.MealSlot
        })
        .IsUnique();
    }
}