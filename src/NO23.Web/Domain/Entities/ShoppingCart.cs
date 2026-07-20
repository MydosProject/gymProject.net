namespace NO23.Web.Domain.Entities;

public class ShoppingCart
{
    public int Id { get; set; }

    public int MemberProfileId { get; set; }

    public MemberProfile MemberProfile { get; set; } = null!;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAtUtc { get; set; }

    public ICollection<CartItem> Items { get; set; } = [];
}
