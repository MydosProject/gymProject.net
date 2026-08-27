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

    public IReadOnlyList<GuestShopVariantViewModel> ShopVariants { get; init; } = [];

    public int? Calories { get; init; }

    public decimal? ProteinGrams { get; init; }

    public decimal? CarbohydrateGrams { get; init; }

    public decimal? FatGrams { get; init; }

    public string? Ingredients { get; init; }

    public string? Allergens { get; init; }

    public IReadOnlyList<KitchenCustomizationOptionViewModel> RemovableIngredients { get; init; } = [];

    public IReadOnlyList<KitchenCustomizationOptionViewModel> AdditionalIngredients { get; init; } = [];

    public bool IsPaymentAvailable { get; init; }

    public string ClubPickupDisplayName { get; init; } = "NO23 Sports Club";

    public GuestOrderInputViewModel Input { get; init; } = new();
}

public class GuestShopVariantViewModel
{
    public int Id { get; init; }

    public string Size { get; init; } = string.Empty;

    public int StockQuantity { get; init; }
}
