using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NO23.Web.Domain.Entities;

namespace NO23.Web.Data.Configurations;

public class KitchenStockMovementConfiguration : IEntityTypeConfiguration<KitchenStockMovement>
{
    public void Configure(EntityTypeBuilder<KitchenStockMovement> builder)
    {
        builder.Property(movement => movement.Type)
            .HasConversion<string>()
            .HasMaxLength(40);

        builder.Property(movement => movement.Quantity)
            .HasPrecision(12, 3);

        builder.Property(movement => movement.QuantityBeforeSnapshot)
            .HasPrecision(12, 3);

        builder.Property(movement => movement.QuantityAfterSnapshot)
            .HasPrecision(12, 3);

        builder.Property(movement => movement.Note)
            .HasMaxLength(500);

        builder.Property(movement => movement.CreatedAtUtc)
            .HasDefaultValueSql("NOW()");

        builder.HasOne(movement => movement.KitchenIngredient)
            .WithMany(ingredient => ingredient.StockMovements)
            .HasForeignKey(movement => movement.KitchenIngredientId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(movement => movement.KitchenProductionPlan)
            .WithMany(plan => plan.StockMovements)
            .HasForeignKey(movement => movement.KitchenProductionPlanId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
