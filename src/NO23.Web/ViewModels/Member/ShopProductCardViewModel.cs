namespace NO23.Web.ViewModels.Member;

public class ShopProductCardViewModel
{
    public int Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public string Category { get; init; } = string.Empty;

    public decimal UnitPrice { get; init; }

    public int StockQuantity { get; init; }

    public string? Description { get; init; }

    public string? ImageUrl { get; init; }

    public string? Tags { get; init; }
}
