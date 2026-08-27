using NO23.Web.Domain.Enums;

namespace NO23.Web.Domain.Entities;

public class OrderItem
{
    public int Id { get; set; }

    public int OrderId { get; set; }

    public Order Order { get; set; } = null!;

    public CartItemType ItemType { get; set; }

    public int? KitchenMenuItemId { get; set; }

    public KitchenMenuItem? KitchenMenuItem { get; set; }

    public int? ShopProductId { get; set; }

    public ShopProduct? ShopProduct { get; set; }

    public int? ShopProductVariantId { get; set; }

    public ShopProductVariant? ShopProductVariant { get; set; }

    public string? SelectedSize { get; set; }

    public string? RemovedIngredientNames { get; set; }

    public string? AddedIngredientNames { get; set; }

    public string ProductName { get; set; } = string.Empty;

    public decimal UnitPrice { get; set; }

    public int Quantity { get; set; }

    public decimal LineTotal { get; set; }
}
