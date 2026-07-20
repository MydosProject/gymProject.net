namespace NO23.Web.Domain.Entities;

public class ShopProduct
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Sku { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string Category { get; set; } = string.Empty;

    public decimal UnitPrice { get; set; }

    public int StockQuantity { get; set; }

    public string? ImageUrl { get; set; }

    public string? Tags { get; set; }

    public bool IsActive { get; set; } = true;

    public int DisplayOrder { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAtUtc { get; set; }

    public ICollection<CartItem> CartItems { get; set; } = [];

    public ICollection<OrderItem> OrderItems { get; set; } = [];
}
