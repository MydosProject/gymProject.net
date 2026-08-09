namespace NO23.Web.ViewModels.Member;

public class KitchenMealPlanViewModel
{
    public int Id { get; init; }

    public string Status { get; init; } = string.Empty;

    public DateTime GeneratedAtUtc { get; init; }

    public IReadOnlyList<KitchenMealPlanDayViewModel> Days { get; init; } = [];
}

public class KitchenMealPlanDayViewModel
{
    public int Id { get; init; }

    public int DayNumber { get; init; }

    public DateOnly PlanDate { get; init; }

    public string DeliveryMethod { get; init; } = string.Empty;

    public string DeliveryMethodDisplayName { get; init; } = string.Empty;

    public string? DeliveryFullName { get; init; }

    public string? DeliveryPhoneNumber { get; init; }

    public string? DeliveryAddressLine { get; init; }

    public string? DeliveryDistrict { get; init; }

    public string? DeliveryCity { get; init; }

    public string? DeliveryPostalCode { get; init; }

    public int TotalCalories { get; init; }

    public decimal TotalProteinGrams { get; init; }

    public decimal TotalCarbohydrateGrams { get; init; }

    public decimal TotalFatGrams { get; init; }

    public decimal TotalPrice { get; init; }

    public bool CanSkip { get; init; }

    public int ActiveMealCount => Meals.Count(meal => !meal.IsSkipped);

    public int SkippedMealCount => Meals.Count(meal => meal.IsSkipped);

    public bool IsFullySkipped => Meals.Count > 0 && ActiveMealCount == 0;

    public IReadOnlyList<KitchenMealPlanMealViewModel> Meals { get; init; } = [];
}

public class KitchenMealPlanMealViewModel
{
    public int Id { get; init; }

    public int KitchenMenuItemId { get; init; }

    public string MealSlot { get; init; } = string.Empty;

    public string MealSlotDisplayName { get; init; } = string.Empty;

    public int Quantity { get; init; }

    public string ProductName { get; init; } = string.Empty;

    public int Calories { get; init; }

    public decimal ProteinGrams { get; init; }

    public decimal CarbohydrateGrams { get; init; }

    public decimal FatGrams { get; init; }

    public decimal UnitPrice { get; init; }

    public decimal TotalPrice { get; init; }

    public bool IsSkipped { get; init; }

    public bool CanSkip { get; init; }
}
