using NO23.Web.Domain.Entities;
using NO23.Web.Domain.Enums;
using NO23.Web.Services;

namespace NO23.Tests;

public class KitchenMealSelectionTests
{
    [Fact]
    public void EmptyOrInvalidSelections_AreRejected()
    {
        Assert.False(KitchenMealSelection.TryCreateMask([], out _));
        Assert.False(KitchenMealSelection.TryCreateMask([(KitchenMealSlot)99], out _));
        Assert.True(KitchenMealSelection.TryCreateMask([KitchenMealSlot.Lunch, KitchenMealSlot.Lunch], out var mask));
        Assert.Equal(4, mask);
    }

    [Fact]
    public void LunchOnly_GeneratesOnlyLunchWithoutPackingFullDayCaloriesIntoIt()
    {
        var subscription = new KitchenSubscription
        { Plan = KitchenSubscriptionPlan.FiveDays, StartsOn = new DateOnly(2026, 10, 1), EndsOn = new DateOnly(2026, 10, 5),
          SelectedMealSlotsMask = 4, DailyCalories = 2000, ProteinGrams = 100, CarbohydrateGrams = 200, FatGrams = 60 };
        var menu = new KitchenMenuItem
        { Id = 1, Name = "Öğle", Category = MenuItemCategory.MainMeal, Calories = 600, ProteinGrams = 30,
          CarbohydrateGrams = 60, FatGrams = 18, IsActive = true, IsPlanEligible = true };
        var match = KitchenPlanMatcher.Match(subscription, [menu]);
        Assert.Equal(KitchenMealPlanStatus.Generated, match.Status);
        Assert.Equal(5, match.Days.Count);
        Assert.All(match.Days, day =>
        {
            var item = Assert.Single(day.Items);
            Assert.Equal(KitchenMealSlot.Lunch, item.MealSlot);
            Assert.Equal(1, item.Quantity);
        });
    }
}
