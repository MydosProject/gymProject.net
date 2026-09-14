using NO23.Web.Domain.Enums;
using NO23.Web.ViewModels.Member;

namespace NO23.Web.Services;

public static class PendingKitchenSubscription
{
    public const string SessionKey = "NO23.Kitchen.PendingSubscription";
}

public sealed record PendingKitchenSubscriptionData(
    KitchenSubscriptionPlan Plan,
    KitchenMealSlot[] SelectedMeals,
    OrderDeliveryMethod DeliveryMethod,
    CalorieCalculatorInputViewModel? CalculatorInput,
    CalorieRecommendationViewModel? Recommendation);
