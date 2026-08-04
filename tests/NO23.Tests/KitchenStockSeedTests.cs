using NO23.Web.Data.Seed;

namespace NO23.Tests;

public class KitchenStockSeedTests
{
    private static readonly string[] LegacyKitchenMenuItemNames =
    [
        "Protein Power Bowl",
        "Lean Breakfast Plate",
        "Green Recovery Smoothie",
        "Gluten Free Fit Brownie",
        "Veggie Crunch Snack Box"
    ];

    [Fact]
    public void Recipes_CoverEveryDefaultKitchenMenuItem()
    {
        var recipeMenuNames = KitchenStockSeed.Recipes
            .Select(recipe => recipe.KitchenMenuItemName)
            .ToHashSet();

        foreach (var menuItem in KitchenMenuItemSeed.Defaults)
        {
            Assert.Contains(menuItem.Name, recipeMenuNames);
        }
    }

    [Fact]
    public void DefaultKitchenMenuItems_HaveUniqueNames()
    {
        Assert.All(
            KitchenMenuItemSeed.Defaults.GroupBy(item => item.Name),
            group => Assert.Single(group));
    }

    [Fact]
    public void Recipes_CoverLegacyKitchenMenuItems()
    {
        var recipeMenuNames = KitchenStockSeed.Recipes
            .Select(recipe => recipe.KitchenMenuItemName)
            .ToHashSet();

        foreach (var menuItemName in LegacyKitchenMenuItemNames)
        {
            Assert.Contains(menuItemName, recipeMenuNames);
        }
    }

    [Fact]
    public void Recipes_ReferenceSeededIngredients()
    {
        var ingredientNames = KitchenStockSeed.Ingredients
            .Select(ingredient => ingredient.Name)
            .ToHashSet();

        foreach (var recipe in KitchenStockSeed.Recipes)
        {
            Assert.Contains(recipe.IngredientName, ingredientNames);
        }
    }

    [Fact]
    public void Ingredients_HaveUniqueNames()
    {
        Assert.All(
            KitchenStockSeed.Ingredients.GroupBy(ingredient => ingredient.Name),
            group => Assert.Single(group));
    }

    [Fact]
    public void Ingredients_HaveEnoughInitialStockForMinimumThresholds()
    {
        foreach (var ingredient in KitchenStockSeed.Ingredients)
        {
            Assert.True(ingredient.CurrentStockQuantity >= ingredient.MinimumStockQuantity);
            Assert.True(ingredient.CurrentStockQuantity > 0);
        }
    }

    [Fact]
    public void Recipes_HavePositiveQuantities()
    {
        foreach (var recipe in KitchenStockSeed.Recipes)
        {
            Assert.True(recipe.QuantityPerPortion > 0);
        }
    }
}
