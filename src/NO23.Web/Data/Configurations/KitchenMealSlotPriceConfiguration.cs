using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NO23.Web.Domain.Entities;

namespace NO23.Web.Data.Configurations;

public class KitchenMealSlotPriceConfiguration
    : IEntityTypeConfiguration<KitchenMealSlotPrice>
{
    public void Configure(
        EntityTypeBuilder<KitchenMealSlotPrice> builder)
    {
        builder.HasKey(price => price.Id);

        builder.Property(price => price.MealSlot)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(price => price.DailyPrice)
            .HasPrecision(10, 2)
            .IsRequired();

        builder.Property(price => price.IsActive)
            .HasDefaultValue(true)
            .IsRequired();

        builder.Property(price => price.CreatedAtUtc)
            .IsRequired();

        builder.HasIndex(price => price.MealSlot)
            .IsUnique();
    }
}