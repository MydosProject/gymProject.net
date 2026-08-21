namespace NO23.Web.Domain.Entities;

public class MemberAllergen
{
    public int MemberProfileId { get; set; }
    public MemberProfile MemberProfile { get; set; } = null!;
    public int KitchenAllergenId { get; set; }
    public KitchenAllergen KitchenAllergen { get; set; } = null!;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
