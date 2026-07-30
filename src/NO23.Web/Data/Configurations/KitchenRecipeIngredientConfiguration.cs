using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NO23.Web.Domain.Entities;

namespace NO23.Web.Data.Configurations;

public class KitchenRecipeIngredientConfiguration : IEntityTypeConfiguration<KitchenRecipeIngredient>
{
    public void Configure(EntityTypeBuilder<KitchenRecipeIngredient> builder)
    {
        builder.Property(recipe => recipe.QuantityPerPortion)
            .HasPrecision(12, 3);

        builder.Property(recipe => recipe.CreatedAtUtc)
            .HasDefaultValueSql("NOW()");

        builder.HasIndex(recipe => new { recipe.KitchenMenuItemId, recipe.KitchenIngredientId })
            .IsUnique();

        builder.HasOne(recipe => recipe.KitchenMenuItem)
            .WithMany(menuItem => menuItem.RecipeIngredients)
            .HasForeignKey(recipe => recipe.KitchenMenuItemId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(recipe => recipe.KitchenIngredient)
            .WithMany(ingredient => ingredient.RecipeIngredients)
            .HasForeignKey(recipe => recipe.KitchenIngredientId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
