using NO23.Web.Domain.Enums;

namespace NO23.Web.ViewModels.Member;

public class KitchenCustomizeViewModel
{
    public KitchenSubscriptionPlan Plan { get; set; }

    public string PackageName { get; set; } = string.Empty;

    public int PackageDays { get; set; }

    public int DailyCalories { get; set; }

    public int ProteinGrams { get; set; }

    public int CarbohydrateGrams { get; set; }

    public int FatGrams { get; set; }

    public List<KitchenMealSelectionOptionViewModel> MealOptions { get; set; } = [];
}

public class KitchenMealSelectionOptionViewModel
{
    public KitchenMealSlot MealSlot { get; set; }

    public string DisplayName { get; set; } = string.Empty;

    public decimal DailyPrice { get; set; }

    public decimal CalorieRatio { get; set; }

    public int TargetCalories { get; set; }

    public bool IsSelected { get; set; }
}