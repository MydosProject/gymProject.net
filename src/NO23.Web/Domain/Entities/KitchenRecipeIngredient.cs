namespace NO23.Web.Domain.Entities;

public class KitchenRecipeIngredient
{
    public int Id { get; set; }

    public int KitchenMenuItemId { get; set; }

    public KitchenMenuItem KitchenMenuItem { get; set; } = null!;

    public int KitchenIngredientId { get; set; }

    public KitchenIngredient KitchenIngredient { get; set; } = null!;

    public decimal QuantityPerPortion { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAtUtc { get; set; }
}
