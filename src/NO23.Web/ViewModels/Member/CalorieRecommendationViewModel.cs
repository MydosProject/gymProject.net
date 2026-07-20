using NO23.Web.Domain.Enums;

namespace NO23.Web.ViewModels.Member;

public class CalorieRecommendationViewModel
{
    public NutritionGoal Goal { get; init; }

    public int DailyCalories { get; init; }

    public int ProteinGrams { get; init; }

    public int CarbohydrateGrams { get; init; }

    public int FatGrams { get; init; }
}
