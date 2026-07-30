using NO23.Web.Domain.Enums;

namespace NO23.Web.Domain.Entities;

public class KitchenIngredient
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public KitchenIngredientUnit Unit { get; set; } = KitchenIngredientUnit.Gram;

    public decimal CurrentStockQuantity { get; set; }

    public decimal MinimumStockQuantity { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAtUtc { get; set; }

    public ICollection<KitchenRecipeIngredient> RecipeIngredients { get; set; } = [];

    public ICollection<KitchenProductionPlanMaterial> ProductionPlanMaterials { get; set; } = [];

    public ICollection<KitchenStockMovement> StockMovements { get; set; } = [];
}
