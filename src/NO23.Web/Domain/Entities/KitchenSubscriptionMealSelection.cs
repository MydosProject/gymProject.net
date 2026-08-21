using NO23.Web.Domain.Enums;

namespace NO23.Web.Domain.Entities;

public class KitchenSubscriptionMealSelection
{
    public int Id { get; set; }

    public int KitchenSubscriptionId { get; set; }

    public KitchenSubscription KitchenSubscription { get; set; } = null!;

    public KitchenMealSlot MealSlot { get; set; }

    public decimal DailyPriceSnapshot { get; set; }

    public decimal CalorieRatioSnapshot { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}