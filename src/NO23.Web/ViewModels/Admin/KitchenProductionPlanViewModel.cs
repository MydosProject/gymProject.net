namespace NO23.Web.ViewModels.Admin;

public class KitchenProductionPlanViewModel
{
    public int Id { get; init; }

    public DateOnly PlanDate { get; init; }

    public DateTime? StockDeductedAtUtc { get; init; }

    public string Status { get; init; } = string.Empty;

    public string StatusDisplayName { get; init; } = string.Empty;

    public bool IsCompleted { get; init; }

    public IReadOnlyList<KitchenProductionPlanItemViewModel> Items { get; init; } = [];

    public IReadOnlyList<KitchenProductionPlanMaterialViewModel> Materials { get; init; } = [];

    public int TotalSubscriptionPortions => Items.Sum(item => item.SubscriptionPortions);

    public int TotalOrderPortions => Items.Sum(item => item.OrderPortions);

    public int TotalPortions => Items.Sum(item => item.TotalPortions);

    public int MissingRecipeCount => Items.Count(item => !item.HasRecipe);

    public int MissingMaterialCount => Materials.Count(item => item.HasMissingStock);
}
