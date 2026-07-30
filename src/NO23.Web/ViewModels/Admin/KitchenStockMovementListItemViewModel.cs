namespace NO23.Web.ViewModels.Admin;

public class KitchenStockMovementListItemViewModel
{
    public string IngredientName { get; init; } = string.Empty;

    public string Type { get; init; } = string.Empty;

    public decimal Quantity { get; init; }

    public string Unit { get; init; } = string.Empty;

    public decimal QuantityBefore { get; init; }

    public decimal QuantityAfter { get; init; }

    public string? Note { get; init; }

    public DateTime CreatedAtUtc { get; init; }
}
