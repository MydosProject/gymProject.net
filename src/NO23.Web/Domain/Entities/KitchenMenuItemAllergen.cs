namespace NO23.Web.Domain.Entities;

public class KitchenMenuItemAllergen
{
    public int KitchenMenuItemId { get; set; }
    public KitchenMenuItem KitchenMenuItem { get; set; } = null!;
    public int KitchenAllergenId { get; set; }
    public KitchenAllergen KitchenAllergen { get; set; } = null!;
}
