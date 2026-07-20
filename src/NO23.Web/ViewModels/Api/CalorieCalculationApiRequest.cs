using NO23.Web.Domain.Enums;

namespace NO23.Web.ViewModels.Api;

public class CalorieCalculationApiRequest
{
    public int HeightCm { get; init; }

    public decimal WeightKg { get; init; }

    public int Age { get; init; }

    public Gender Gender { get; init; }

    public ActivityLevel ActivityLevel { get; init; }

    public NutritionGoal Goal { get; init; }
}
