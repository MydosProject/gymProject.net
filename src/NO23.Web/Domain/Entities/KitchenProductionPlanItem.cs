using NO23.Web.Domain.Enums;

namespace NO23.Web.Domain.Entities;

public class KitchenProductionPlanItem
{
    public int Id { get; set; }

    public int KitchenProductionPlanId { get; set; }

    public KitchenProductionPlan KitchenProductionPlan { get; set; } = null!;

    public int KitchenMenuItemId { get; set; }

    public KitchenMenuItem KitchenMenuItem { get; set; } = null!;

    public string ProductNameSnapshot { get; set; } = string.Empty;

    public int SubscriptionPortions { get; set; }

    public int OrderPortions { get; set; }

    public int TotalPortions { get; set; }

    public bool HasRecipeSnapshot { get; set; }

    public KitchenProductionItemStatus Status { get; set; } = KitchenProductionItemStatus.NotStarted;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAtUtc { get; set; }
}
