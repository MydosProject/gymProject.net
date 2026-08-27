namespace NO23.Web.Domain.Entities;

public class ShopProductVariant
{
    public int Id { get; set; }

    public int ShopProductId { get; set; }

    public ShopProduct ShopProduct { get; set; } = null!;

    public string Size { get; set; } = string.Empty;

    public int StockQuantity { get; set; }

    public bool IsActive { get; set; } = true;

    public int DisplayOrder { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAtUtc { get; set; }

    public ICollection<CartItem> CartItems { get; set; } = [];

    public ICollection<OrderItem> OrderItems { get; set; } = [];
}
