namespace NO23.Web.ViewModels.Member;

public class KitchenDashboardViewModel
{
    public CalorieCalculatorInputViewModel CalculatorInput { get; init; } = new();

    public CalorieRecommendationViewModel? Recommendation { get; init; }

    public IReadOnlyList<KitchenMenuItemCardViewModel> MenuItems { get; init; } = [];

    public IReadOnlyList<KitchenSubscriptionPlanViewModel> SubscriptionPlans { get; init; } = [];
}
