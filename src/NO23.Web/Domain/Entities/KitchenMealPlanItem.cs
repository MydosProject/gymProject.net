using NO23.Web.Domain.Enums;

namespace NO23.Web.Domain.Entities;

public class KitchenMealPlanItem
{
    public int Id { get; set; }

    public int KitchenMealPlanDayId { get; set; }

    public KitchenMealPlanDay KitchenMealPlanDay { get; set; } = null!;

    public int KitchenMenuItemId { get; set; }

    public KitchenMenuItem KitchenMenuItem { get; set; } = null!;

    public KitchenMealSlot MealSlot { get; set; }

    public int Quantity { get; set; } = 1;

    public string ProductNameSnapshot { get; set; } = string.Empty;

    public int CaloriesSnapshot { get; set; }

    public decimal ProteinGramsSnapshot { get; set; }

    public decimal CarbohydrateGramsSnapshot { get; set; }

    public decimal FatGramsSnapshot { get; set; }

    public decimal UnitPriceSnapshot { get; set; }

    public bool IsSkipped { get; set; }

    public DateTime? SkippedAtUtc { get; set; }
}
