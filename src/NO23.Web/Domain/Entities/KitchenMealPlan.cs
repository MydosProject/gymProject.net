using NO23.Web.Domain.Enums;

namespace NO23.Web.Domain.Entities;

public class KitchenMealPlan
{
    public int Id { get; set; }

    public int KitchenSubscriptionId { get; set; }

    public KitchenSubscription KitchenSubscription { get; set; } = null!;

    public KitchenMealPlanStatus Status { get; set; } = KitchenMealPlanStatus.Generated;

    public string CalculationVersion { get; set; } = "v1";

    public int SourceHeightCm { get; set; }

    public decimal SourceWeightKg { get; set; }

    public int SourceAge { get; set; }

    public Gender SourceGender { get; set; }

    public ActivityLevel SourceActivityLevel { get; set; }

    public NutritionGoal SourceGoal { get; set; }

    public int TargetDailyCalories { get; set; }

    public int TargetProteinGrams { get; set; }

    public int TargetCarbohydrateGrams { get; set; }

    public int TargetFatGrams { get; set; }

    public DateTime GeneratedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAtUtc { get; set; }

    public ICollection<KitchenMealPlanDay> Days { get; set; } = [];
}
