namespace NO23.Web.Domain.Entities;

public class KitchenAllergen
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public int DisplayOrder { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }
    public ICollection<KitchenMenuItemAllergen> MenuItems { get; set; } = [];
    public ICollection<MemberAllergen> Members { get; set; } = [];
}
