using NO23.Web.Domain.Enums;

namespace NO23.Web.Domain.Entities;

public class KitchenProductionPlan
{
    public int Id { get; set; }

    public DateOnly PlanDate { get; set; }

    public KitchenProductionPlanStatus Status { get; set; } = KitchenProductionPlanStatus.Draft;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAtUtc { get; set; }

    public DateTime? StockDeductedAtUtc { get; set; }

    public ICollection<KitchenProductionPlanItem> Items { get; set; } = [];

    public ICollection<KitchenProductionPlanMaterial> Materials { get; set; } = [];

    public ICollection<KitchenStockMovement> StockMovements { get; set; } = [];
}
