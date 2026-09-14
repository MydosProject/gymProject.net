using NO23.Web.ViewModels.GuestOrders;
using NO23.Web.ViewModels.Member;

namespace NO23.Web.ViewModels;

public class KitchenPublicPageViewModel
{
    public IReadOnlyList<GuestOrderPageViewModel> MenuItems { get; init; } = [];

    public IReadOnlyList<KitchenSubscriptionPlanViewModel> SubscriptionPlans { get; init; } = [];

    public CalorieCalculatorInputViewModel CalculatorInput { get; init; } = new();

    public CalorieRecommendationViewModel? Recommendation { get; init; }
}
