using NO23.Web.Domain.Enums;

namespace NO23.Web.Domain.Entities;

public class KitchenMealSlotPrice
{
    public int Id { get; set; }

    public KitchenMealSlot MealSlot { get; set; }

    public decimal DailyPrice { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAtUtc { get; set; }
}