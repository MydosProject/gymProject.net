namespace NO23.Web.ViewModels.Admin;

public class KitchenIngredientListItemViewModel
{
    public int Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public string Unit { get; init; } = string.Empty;

    public decimal CurrentStockQuantity { get; init; }

    public decimal MinimumStockQuantity { get; init; }

    public bool IsActive { get; init; }

    public bool IsBelowMinimum => CurrentStockQuantity < MinimumStockQuantity;
}
