namespace NO23.Web.ViewModels.GuestOrders;

public class GuestOrderPageViewModel
{
    public int ItemId { get; init; }

    public string ItemName { get; init; } = string.Empty;

    public string? Description { get; init; }

    public string Category { get; init; } = string.Empty;

    public decimal UnitPrice { get; init; }

    public string? ImageUrl { get; init; }

    public int? StockQuantity { get; init; }

    public int? Calories { get; init; }

    public decimal? ProteinGrams { get; init; }

    public decimal? CarbohydrateGrams { get; init; }

    public decimal? FatGrams { get; init; }

    public string? Ingredients { get; init; }

    public string? Allergens { get; init; }

    public GuestOrderInputViewModel Input { get; init; } = new();
}
