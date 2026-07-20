using NO23.Web.Domain.Enums;
using NO23.Web.Services;

namespace NO23.Tests;

public class CalorieCalculatorServiceTests
{
    private readonly CalorieCalculatorService calculator = new();

    [Fact]
    public void Calculate_ReturnsHigherCaloriesForMuscleGainThanMaintenance()
    {
        var maintenance = calculator.Calculate(new CalorieCalculationRequest
        {
            HeightCm = 180,
            WeightKg = 80,
            Age = 30,
            Gender = Gender.Male,
            ActivityLevel = ActivityLevel.ModeratelyActive,
            Goal = NutritionGoal.WeightMaintenance
        });

        var muscleGain = calculator.Calculate(new CalorieCalculationRequest
        {
            HeightCm = 180,
            WeightKg = 80,
            Age = 30,
            Gender = Gender.Male,
            ActivityLevel = ActivityLevel.ModeratelyActive,
            Goal = NutritionGoal.MuscleGain
        });

        Assert.True(muscleGain.DailyCalories > maintenance.DailyCalories);
        Assert.True(muscleGain.ProteinGrams > maintenance.ProteinGrams);
    }

    [Fact]
    public void Calculate_ReturnsLowerCaloriesForFatLossThanMaintenance()
    {
        var maintenance = calculator.Calculate(new CalorieCalculationRequest
        {
            HeightCm = 165,
            WeightKg = 62,
            Age = 28,
            Gender = Gender.Female,
            ActivityLevel = ActivityLevel.LightlyActive,
            Goal = NutritionGoal.WeightMaintenance
        });

        var fatLoss = calculator.Calculate(new CalorieCalculationRequest
        {
            HeightCm = 165,
            WeightKg = 62,
            Age = 28,
            Gender = Gender.Female,
            ActivityLevel = ActivityLevel.LightlyActive,
            Goal = NutritionGoal.FatLoss
        });

        Assert.True(fatLoss.DailyCalories < maintenance.DailyCalories);
        Assert.True(fatLoss.ProteinGrams > 0);
        Assert.True(fatLoss.CarbohydrateGrams > 0);
        Assert.True(fatLoss.FatGrams > 0);
    }

    [Fact]
    public void Calculate_ThrowsForInvalidHeight()
    {
        var request = new CalorieCalculationRequest
        {
            HeightCm = 90,
            WeightKg = 70,
            Age = 30,
            Gender = Gender.Male,
            ActivityLevel = ActivityLevel.ModeratelyActive,
            Goal = NutritionGoal.WeightMaintenance
        };

        Assert.Throws<ArgumentOutOfRangeException>(() => calculator.Calculate(request));
    }
}
