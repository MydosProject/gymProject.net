using NO23.Web.Domain.Enums;

namespace NO23.Web.Domain.Entities;

public class KitchenSubscription
{
    public int Id { get; set; }

    public int MemberProfileId { get; set; }

    public MemberProfile MemberProfile { get; set; } = null!;

    public int KitchenSubscriptionPackageId { get; set; }

    public KitchenSubscriptionPackage KitchenSubscriptionPackage { get; set; } = null!;

    public KitchenSubscriptionPlan Plan { get; set; }

    public KitchenSubscriptionStatus Status { get; set; } =
    KitchenSubscriptionStatus.PendingPayment;

    public string PackageNameSnapshot { get; set; } = string.Empty;

    public decimal PackagePriceSnapshot { get; set; }

    public int PackageDaysSnapshot { get; set; }

    public NutritionGoal Goal { get; set; }

    public int? SourceHeightCm { get; set; }

    public decimal? SourceWeightKg { get; set; }

    public int? SourceAge { get; set; }

    public Gender? SourceGender { get; set; }

    public ActivityLevel? SourceActivityLevel { get; set; }

    public int DailyCalories { get; set; }

    public int ProteinGrams { get; set; }

    public int CarbohydrateGrams { get; set; }

    public int FatGrams { get; set; }

    public DateOnly StartsOn { get; set; }

    public DateOnly EndsOn { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAtUtc { get; set; }

    public ICollection<Order> Orders { get; set; } = [];

    public KitchenMealPlan? MealPlan { get; set; }
}
