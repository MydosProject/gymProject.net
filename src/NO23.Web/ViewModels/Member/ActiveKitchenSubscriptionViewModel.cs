namespace NO23.Web.ViewModels.Member;

public class ActiveKitchenSubscriptionViewModel
{
    public int Id { get; init; }

    public string Plan { get; init; } = string.Empty;

    public string Status { get; init; } = string.Empty;

    public string Goal { get; init; } = string.Empty;

    public string PackageName { get; init; } = string.Empty;

    public decimal PackagePrice { get; init; }

    public int PackageDays { get; init; }

    public int DailyCalories { get; init; }

    public int ProteinGrams { get; init; }

    public int CarbohydrateGrams { get; init; }

    public int FatGrams { get; init; }

    public DateOnly StartsOn { get; init; }

    public DateOnly EndsOn { get; init; }

    public int RemainingDays { get; init; }

    public KitchenMealPlanViewModel? MealPlan { get; init; }

    public IReadOnlyList<KitchenMealPlanDayViewModel> MealPlanDays =>
        MealPlan?.Days ?? [];
}
