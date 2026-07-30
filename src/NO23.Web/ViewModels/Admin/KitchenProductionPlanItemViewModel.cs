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
}
