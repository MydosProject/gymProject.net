namespace NO23.Web.ViewModels.Admin;

public class ShopProductListItemViewModel
{
    public int Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public string Sku { get; init; } = string.Empty;

    public string Category { get; init; } = string.Empty;

    public decimal UnitPrice { get; init; }

    public int StockQuantity { get; init; }

    public int MinimumStockQuantity { get; init; }

    public bool IsActive { get; init; }

    public int DisplayOrder { get; init; }
}
