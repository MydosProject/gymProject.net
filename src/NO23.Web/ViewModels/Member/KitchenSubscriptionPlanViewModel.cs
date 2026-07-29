using NO23.Web.Domain.Enums;

namespace NO23.Web.ViewModels.Member;

public class KitchenSubscriptionPlanViewModel
{
    public KitchenSubscriptionPlan Plan { get; init; }

    public string Name { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public int Days { get; init; }

    public decimal UnitPrice { get; init; }

    public bool IsActive { get; init; }
}
