using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NO23.Web.Domain.Entities;

namespace NO23.Web.Data.Configurations;

public class KitchenProductionPlanMaterialConfiguration : IEntityTypeConfiguration<KitchenProductionPlanMaterial>
{
    public void Configure(EntityTypeBuilder<KitchenProductionPlanMaterial> builder)
    {
        builder.Property(material => material.IngredientNameSnapshot)
            .HasMaxLength(120)
            .IsRequired();

        builder.Property(material => material.UnitSnapshot)
            .HasConversion<string>()
            .HasMaxLength(40);

        builder.Property(material => material.RequiredQuantity)
            .HasPrecision(12, 3);

        builder.Property(material => material.StockQuantitySnapshot)
            .HasPrecision(12, 3);

        builder.Property(material => material.MissingQuantity)
            .HasPrecision(12, 3);

        builder.HasIndex(material => new { material.KitchenProductionPlanId, material.KitchenIngredientId })
            .IsUnique();

        builder.HasOne(material => material.KitchenProductionPlan)
            .WithMany(plan => plan.Materials)
            .HasForeignKey(material => material.KitchenProductionPlanId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(material => material.KitchenIngredient)
            .WithMany(ingredient => ingredient.ProductionPlanMaterials)
            .HasForeignKey(material => material.KitchenIngredientId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
