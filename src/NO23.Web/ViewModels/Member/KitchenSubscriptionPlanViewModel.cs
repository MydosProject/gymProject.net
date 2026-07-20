using NO23.Web.Domain.Enums;

namespace NO23.Web.ViewModels.Member;

public class KitchenSubscriptionPlanViewModel
{
    public KitchenSubscriptionPlan Plan { get; init; }

    public string Name { get; init; } = string.Empty;

    public int Days { get; init; }
}
