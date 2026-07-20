using NO23.Web.Domain.Enums;

namespace NO23.Web.Services;

public class CalorieCalculatorService
{
    public CalorieCalculationResult Calculate(CalorieCalculationRequest request)
    {
        Validate(request);

        var basalMetabolicRate = CalculateBasalMetabolicRate(request);
        var maintenanceCalories = basalMetabolicRate * GetActivityMultiplier(request.ActivityLevel);
        var targetCalories = maintenanceCalories * GetGoalCalorieMultiplier(request.Goal);
        var roundedCalories = RoundToNearestTen(targetCalories);

        var proteinGrams = (int)Math.Round(request.WeightKg * GetProteinMultiplier(request.Goal));
        var fatCalories = roundedCalories * GetFatCalorieRatio(request.Goal);
        var fatGrams = (int)Math.Round(fatCalories / 9);
        var carbohydrateCalories = roundedCalories - (proteinGrams * 4) - (fatGrams * 9);
        var carbohydrateGrams = Math.Max(0, (int)Math.Round(carbohydrateCalories / 4m));

        return new CalorieCalculationResult
        {
            DailyCalories = roundedCalories,
            ProteinGrams = proteinGrams,
            CarbohydrateGrams = carbohydrateGrams,
            FatGrams = fatGrams
        };
    }

    private static decimal CalculateBasalMetabolicRate(CalorieCalculationRequest request)
    {
        var genderOffset = request.Gender == Gender.Male ? 5 : -161;
        return (10 * request.WeightKg) + (6.25m * request.HeightCm) - (5 * request.Age) + genderOffset;
    }

    private static decimal GetActivityMultiplier(ActivityLevel activityLevel)
    {
        return activityLevel switch
        {
            ActivityLevel.Sedentary => 1.2m,
            ActivityLevel.LightlyActive => 1.375m,
            ActivityLevel.ModeratelyActive => 1.55m,
            ActivityLevel.VeryActive => 1.725m,
            ActivityLevel.Athlete => 1.9m,
            _ => throw new ArgumentOutOfRangeException(nameof(activityLevel), activityLevel, null)
        };
    }

    private static decimal GetGoalCalorieMultiplier(NutritionGoal goal)
    {
        return goal switch
        {
            NutritionGoal.FatLoss => 0.85m,
            NutritionGoal.MuscleGain => 1.10m,
            NutritionGoal.WeightMaintenance => 1.00m,
            NutritionGoal.PerformanceNutrition => 1.15m,
            NutritionGoal.HealthyLifestyle => 1.00m,
            _ => throw new ArgumentOutOfRangeException(nameof(goal), goal, null)
        };
    }

    private static decimal GetProteinMultiplier(NutritionGoal goal)
    {
        return goal switch
        {
            NutritionGoal.FatLoss => 2.0m,
            NutritionGoal.MuscleGain => 2.2m,
            NutritionGoal.WeightMaintenance => 1.6m,
            NutritionGoal.PerformanceNutrition => 1.8m,
            NutritionGoal.HealthyLifestyle => 1.4m,
            _ => throw new ArgumentOutOfRangeException(nameof(goal), goal, null)
        };
    }

    private static decimal GetFatCalorieRatio(NutritionGoal goal)
    {
        return goal switch
        {
            NutritionGoal.FatLoss => 0.30m,
            NutritionGoal.MuscleGain => 0.25m,
            NutritionGoal.WeightMaintenance => 0.28m,
            NutritionGoal.PerformanceNutrition => 0.25m,
            NutritionGoal.HealthyLifestyle => 0.30m,
            _ => throw new ArgumentOutOfRangeException(nameof(goal), goal, null)
        };
    }

    private static int RoundToNearestTen(decimal value)
    {
        return (int)(Math.Round(value / 10, MidpointRounding.AwayFromZero) * 10);
    }

    private static void Validate(CalorieCalculationRequest request)
    {
        if (request.HeightCm is < 120 or > 230)
        {
            throw new ArgumentOutOfRangeException(nameof(request.HeightCm), "Height must be between 120 and 230 cm.");
        }

        if (request.WeightKg is < 35 or > 250)
        {
            throw new ArgumentOutOfRangeException(nameof(request.WeightKg), "Weight must be between 35 and 250 kg.");
        }

        if (request.Age is < 13 or > 90)
        {
            throw new ArgumentOutOfRangeException(nameof(request.Age), "Age must be between 13 and 90.");
        }
    }
}
