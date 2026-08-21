using NO23.Web.Domain.Entities;
using NO23.Web.Domain.Enums;

namespace NO23.Web.Data.Seed;

public static class KitchenMealSlotPriceSeed
{
    public static IReadOnlyList<KitchenMealSlotPrice> Defaults { get; } =
    [
        new()
        {
            MealSlot = KitchenMealSlot.Breakfast,
            DailyPrice = 170m,
            IsActive = true
        },
        new()
        {
            MealSlot = KitchenMealSlot.MorningSnack,
            DailyPrice = 85m,
            IsActive = true
        },
        new()
        {
            MealSlot = KitchenMealSlot.Lunch,
            DailyPrice = 255m,
            IsActive = true
        },
        new()
        {
            MealSlot = KitchenMealSlot.AfternoonSnack,
            DailyPrice = 85m,
            IsActive = true
        },
        new()
        {
            MealSlot = KitchenMealSlot.Dinner,
            DailyPrice = 255m,
            IsActive = true
        }
    ];
}