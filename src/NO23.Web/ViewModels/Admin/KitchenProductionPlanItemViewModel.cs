namespace NO23.Web.ViewModels.Admin;

public class KitchenProductionPlanItemViewModel
{
    public int Id { get; init; }

    public string ProductName { get; init; } = string.Empty;

    public int SubscriptionPortions { get; init; }

    public int OrderPortions { get; init; }

    public int TotalPortions { get; init; }

    public bool HasRecipe { get; init; }

    public string Status { get; init; } = string.Empty;

    public string StatusDisplayName { get; init; } = string.Empty;

    public IReadOnlyList<KitchenProductionPlanRecipeIngredientViewModel> RecipeIngredients { get; init; } = [];
}

public class KitchenProductionPlanRecipeIngredientViewModel
{
    public string IngredientName { get; init; } = string.Empty;

    public string Unit { get; init; } = string.Empty;

    public decimal QuantityPerPortion { get; init; }

    public decimal RequiredQuantity { get; init; }

    public decimal CurrentStockQuantity { get; init; }
}
