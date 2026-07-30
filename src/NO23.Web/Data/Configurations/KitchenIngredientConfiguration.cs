using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NO23.Web.Domain.Entities;

namespace NO23.Web.Data.Configurations;

public class KitchenIngredientConfiguration : IEntityTypeConfiguration<KitchenIngredient>
{
    public void Configure(EntityTypeBuilder<KitchenIngredient> builder)
    {
        builder.Property(ingredient => ingredient.Name)
            .HasMaxLength(120)
            .IsRequired();

        builder.HasIndex(ingredient => ingredient.Name)
            .IsUnique();

        builder.Property(ingredient => ingredient.Unit)
            .HasConversion<string>()
            .HasMaxLength(40);

        builder.Property(ingredient => ingredient.CurrentStockQuantity)
            .HasPrecision(12, 3);

        builder.Property(ingredient => ingredient.MinimumStockQuantity)
            .HasPrecision(12, 3);

        builder.Property(ingredient => ingredient.CreatedAtUtc)
            .HasDefaultValueSql("NOW()");
    }
}
