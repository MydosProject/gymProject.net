namespace NO23.Web.ViewModels.Admin;

public class KitchenStockDashboardViewModel
{
    public DateOnly SelectedDate { get; init; }

    public KitchenProductionPlanViewModel? ProductionPlan { get; init; }

    public IReadOnlyList<KitchenIngredientListItemViewModel> Ingredients { get; init; } = [];

    public IReadOnlyList<KitchenStockMovementListItemViewModel> RecentMovements { get; init; } = [];

    public KitchenIngredientFormViewModel IngredientForm { get; init; } = new();

    public KitchenStockMovementFormViewModel StockMovementForm { get; init; } = new();
}
