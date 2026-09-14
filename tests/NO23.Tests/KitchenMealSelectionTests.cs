using NO23.Web.Domain.Entities;
using NO23.Web.Domain.Enums;
using NO23.Web.Services;

namespace NO23.Tests;

public class KitchenMealSelectionTests
{
    [Theory]
    [InlineData(1, 3000)]
    [InlineData(2, 5500)]
    [InlineData(3, 7500)]
    public void MealCount_UsesPublishedPriceTier(int mainMealCount, decimal expectedPrice)
    {
        var package = new KitchenSubscriptionPackage
        {
            UnitPrice = 3000,
            TwoMainMealsPrice = 5500,
            ThreeMainMealsPrice = 7500
        };
        var mainMeals = new[] { KitchenMealSlot.Breakfast, KitchenMealSlot.Lunch, KitchenMealSlot.Dinner };
        var selections = mainMeals.Take(mainMealCount).Append(KitchenMealSlot.AfternoonSnack);

        var success = KitchenSubscriptionPricing.TryCalculate(package, selections, out var quote, out _);

        Assert.True(success);
        Assert.Equal(expectedPrice, quote!.PackagePrice);
        Assert.Equal(mainMealCount, quote.MainMealCount);
    }

    [Fact]
    public void TwoSnackSlots_AreRejectedBecausePublishedPriceIncludesOneSnack()
    {
        var package = new KitchenSubscriptionPackage
        {
            UnitPrice = 3000,
            TwoMainMealsPrice = 5500,
            ThreeMainMealsPrice = 7500
        };

        Assert.False(KitchenSubscriptionPricing.TryCalculate(
            package,
            [KitchenMealSlot.Breakfast, KitchenMealSlot.MorningSnack, KitchenMealSlot.AfternoonSnack],
            out _,
            out var error));
        Assert.Contains("bir ara öğün", error);
    }

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

    [Fact]
    public void SingleMainMeal_ProducesStandardPortionPlanWithoutCalorieTarget()
    {
        Assert.True(KitchenMealSelection.TryCreateMask(
            [KitchenMealSlot.Lunch, KitchenMealSlot.AfternoonSnack],
            out var selectedMealSlotsMask));
        var subscription = new KitchenSubscription
        {
            Plan = KitchenSubscriptionPlan.FiveDays,
            StartsOn = new DateOnly(2026, 10, 1),
            EndsOn = new DateOnly(2026, 10, 5),
            SelectedMealSlotsMask = selectedMealSlotsMask,
            DailyCalories = 0
        };
        var menuItems = new[]
        {
            new KitchenMenuItem { Id = 1, Name = "Öğle", Category = MenuItemCategory.MainMeal, Calories = 600, IsActive = true, IsPlanEligible = true },
            new KitchenMenuItem { Id = 2, Name = "Ara", Category = MenuItemCategory.Snack, Calories = 180, IsActive = true, IsPlanEligible = true }
        };

        var match = KitchenPlanMatcher.Match(subscription, menuItems);

        Assert.Equal(KitchenMealPlanStatus.Generated, match.Status);
        Assert.All(match.Days, day => Assert.Equal(2, day.Items.Count));
    }
}
