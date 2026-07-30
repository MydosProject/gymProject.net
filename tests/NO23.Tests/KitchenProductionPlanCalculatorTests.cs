using NO23.Web.Domain.Enums;
using NO23.Web.Services;

namespace NO23.Tests;

public class KitchenProductionPlanCalculatorTests
{
    [Fact]
    public void Calculate_CombinesSubscriptionAndOrderPortionsForSameKitchenProduct()
    {
        var draft = KitchenProductionPlanCalculator.Calculate(
            [
                new KitchenProductionDemandRow(1, "Protein Bowl", 3, 0),
                new KitchenProductionDemandRow(1, "Protein Bowl", 0, 2)
            ],
            [
                new KitchenProductionRecipeRow(1, 10, "Tavuk", KitchenIngredientUnit.Gram, 120, 1000)
            ]);

        var item = Assert.Single(draft.Items);
        Assert.Equal(3, item.SubscriptionPortions);
        Assert.Equal(2, item.OrderPortions);
        Assert.Equal(5, item.TotalPortions);
        Assert.True(item.HasRecipe);
    }

    [Fact]
    public void Calculate_ComputesRequiredAndMissingIngredientQuantities()
    {
        var draft = KitchenProductionPlanCalculator.Calculate(
            [
                new KitchenProductionDemandRow(1, "Protein Bowl", 4, 1),
                new KitchenProductionDemandRow(2, "Somon Tabak", 2, 0)
            ],
            [
                new KitchenProductionRecipeRow(1, 10, "Pirinç", KitchenIngredientUnit.Gram, 80, 500),
                new KitchenProductionRecipeRow(2, 10, "Pirinç", KitchenIngredientUnit.Gram, 60, 500)
            ]);

        var material = Assert.Single(draft.Materials);
        Assert.Equal(520, material.RequiredQuantity);
        Assert.Equal(500, material.StockQuantity);
        Assert.Equal(20, material.MissingQuantity);
    }

    [Fact]
    public void Calculate_FlagsKitchenProductsWithoutRecipe()
    {
        var draft = KitchenProductionPlanCalculator.Calculate(
            [
                new KitchenProductionDemandRow(1, "Reçeteli Ürün", 1, 0),
                new KitchenProductionDemandRow(2, "Reçetesiz Ürün", 1, 0)
            ],
            [
                new KitchenProductionRecipeRow(1, 10, "Tavuk", KitchenIngredientUnit.Gram, 100, 500)
            ]);

        var recipeMissingItem = Assert.Single(
            draft.Items,
            item => item.ProductName == "Reçetesiz Ürün");

        Assert.False(recipeMissingItem.HasRecipe);
    }

    [Fact]
    public void CalculateSuggestedStockEntryQuantity_UsesProductionMissingWhenItIsHigher()
    {
        var suggestedQuantity = KitchenProductionPlanCalculator.CalculateSuggestedStockEntryQuantity(
            requiredQuantity: 750,
            currentStockQuantity: 300,
            minimumStockQuantity: 500);

        Assert.Equal(450m, suggestedQuantity);
    }

    [Fact]
    public void CalculateSuggestedStockEntryQuantity_UsesMinimumStockDeficitWhenItIsHigher()
    {
        var suggestedQuantity = KitchenProductionPlanCalculator.CalculateSuggestedStockEntryQuantity(
            requiredQuantity: 100,
            currentStockQuantity: 300,
            minimumStockQuantity: 500);

        Assert.Equal(200m, suggestedQuantity);
    }

    [Fact]
    public void CalculateSuggestedStockEntryQuantity_ReturnsZeroWhenStockIsEnough()
    {
        var suggestedQuantity = KitchenProductionPlanCalculator.CalculateSuggestedStockEntryQuantity(
            requiredQuantity: 100,
            currentStockQuantity: 600,
            minimumStockQuantity: 500);

        Assert.Equal(0m, suggestedQuantity);
    }
}
