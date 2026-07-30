using NO23.Web.Domain.Enums;

namespace NO23.Web.Domain.Entities;

public class KitchenStockMovement
{
    public int Id { get; set; }

    public int KitchenIngredientId { get; set; }

    public KitchenIngredient KitchenIngredient { get; set; } = null!;

    public int? KitchenProductionPlanId { get; set; }

    public KitchenProductionPlan? KitchenProductionPlan { get; set; }

    public KitchenStockMovementType Type { get; set; }

    public decimal Quantity { get; set; }

    public decimal QuantityBeforeSnapshot { get; set; }

    public decimal QuantityAfterSnapshot { get; set; }

    public string? Note { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
