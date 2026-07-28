namespace NO23.Web.Domain.Entities;

public class KitchenMealPlanDay
{
    public int Id { get; set; }

    public int KitchenMealPlanId { get; set; }

    public KitchenMealPlan KitchenMealPlan { get; set; } = null!;

    public int DayNumber { get; set; }

    public DateOnly PlanDate { get; set; }

    public int TotalCalories { get; set; }

    public decimal TotalProteinGrams { get; set; }

    public decimal TotalCarbohydrateGrams { get; set; }

    public decimal TotalFatGrams { get; set; }

    public ICollection<KitchenMealPlanItem> Items { get; set; } = [];
}
