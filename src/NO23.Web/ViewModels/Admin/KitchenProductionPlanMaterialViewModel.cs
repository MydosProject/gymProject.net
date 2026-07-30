namespace NO23.Web.ViewModels.Admin;

public class KitchenProductionPlanMaterialViewModel
{
    public string IngredientName { get; init; } = string.Empty;

    public string Unit { get; init; } = string.Empty;

    public decimal RequiredQuantity { get; init; }

    public decimal StockQuantity { get; init; }

    public decimal MinimumStockQuantity { get; init; }

    public decimal MissingQuantity { get; init; }

    public decimal SuggestedStockEntryQuantity { get; init; }

    public bool HasMissingStock => MissingQuantity > 0;

    public bool HasSuggestedStockEntry => SuggestedStockEntryQuantity > 0;
}
