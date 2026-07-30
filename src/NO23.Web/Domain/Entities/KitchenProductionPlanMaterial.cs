using NO23.Web.Domain.Enums;

namespace NO23.Web.Domain.Entities;

public class KitchenProductionPlanMaterial
{
    public int Id { get; set; }

    public int KitchenProductionPlanId { get; set; }

    public KitchenProductionPlan KitchenProductionPlan { get; set; } = null!;

    public int KitchenIngredientId { get; set; }

    public KitchenIngredient KitchenIngredient { get; set; } = null!;

    public string IngredientNameSnapshot { get; set; } = string.Empty;

    public KitchenIngredientUnit UnitSnapshot { get; set; }

    public decimal RequiredQuantity { get; set; }

    public decimal StockQuantitySnapshot { get; set; }

    public decimal MissingQuantity { get; set; }
}
