namespace NO23.Web.ViewModels.Member;

public class KitchenDashboardViewModel
{
    public CalorieCalculatorInputViewModel CalculatorInput { get; init; } = new();

    public CalorieRecommendationViewModel? Recommendation { get; init; }

    public ActiveKitchenSubscriptionViewModel? ActiveSubscription { get; init; }

    public IReadOnlyList<KitchenMenuItemCardViewModel> MenuItems { get; init; } = [];

    public IReadOnlyList<KitchenFilterOptionViewModel> CategoryFilters { get; init; } = [];

    public IReadOnlyList<KitchenFilterOptionViewModel> TagFilters { get; init; } = [];

    public IReadOnlyList<string> MemberAllergenNames { get; init; } = [];

    public IReadOnlyList<KitchenSubscriptionPlanViewModel> SubscriptionPlans { get; init; } = [];

    public string ClubPickupDisplayName { get; init; } = "NO23 Sports Club";
}
