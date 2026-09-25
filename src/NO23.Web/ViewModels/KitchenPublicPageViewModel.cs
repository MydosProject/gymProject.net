using NO23.Web.ViewModels.GuestOrders;
using NO23.Web.ViewModels.Member;
using NO23.Web.Domain.Enums;

namespace NO23.Web.ViewModels;

public class KitchenPublicPageViewModel
{
    public IReadOnlyList<GuestOrderPageViewModel> MenuItems { get; init; } = [];

    public IReadOnlyList<KitchenSubscriptionPlanViewModel> SubscriptionPlans { get; init; } = [];

    public CalorieCalculatorInputViewModel CalculatorInput { get; init; } = new();

    public CalorieRecommendationViewModel? Recommendation { get; init; }
}

public class KitchenSubscriptionAuthChoiceViewModel
{
    public string PackageName { get; init; } = string.Empty;

    public int PackageDays { get; init; }

    public string SelectedMeals { get; init; } = string.Empty;

    public int DailyCalories { get; init; }

    public decimal PackagePrice { get; init; }

    public decimal DeliveryPrice { get; init; }

    public decimal TotalPrice { get; init; }

    public OrderDeliveryMethod DeliveryMethod { get; init; }

    public string ResumeUrl { get; init; } = string.Empty;
}
