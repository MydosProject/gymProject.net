using NO23.Web.Data.Seed;
using NO23.Web.Domain.Entities;
using NO23.Web.Domain.Enums;
using NO23.Web.Services;

namespace NO23.Tests;

public class KitchenPlanMatcherTests
{
    [Fact]
    public void Match_GeneratesFiveMealDailyPlan()
    {
        var subscription = BuildSubscription(
            dailyCalories: 1900,
            proteinGrams: 140,
            carbohydrateGrams: 190,
            fatGrams: 63);

        var match = KitchenPlanMatcher.Match(subscription, BuildMenuItems());

        Assert.Equal(KitchenMealPlanStatus.Generated, match.Status);
        Assert.Equal(5, match.Days.Count);

        foreach (var day in match.Days)
        {
            Assert.InRange(day.TotalCalories, 1710, 2090);
            Assert.Equal(5, day.Items.Count);
            Assert.Equal(
                [
                    KitchenMealSlot.Breakfast,
                    KitchenMealSlot.MorningSnack,
                    KitchenMealSlot.Lunch,
                    KitchenMealSlot.AfternoonSnack,
                    KitchenMealSlot.Dinner
                ],
                day.Items.Select(item => item.MealSlot).ToArray());
        }
    }

    [Fact]
    public void Match_MapsSlotsToExpectedKitchenMenuCategories()
    {
        var match = KitchenPlanMatcher.Match(BuildSubscription(), BuildMenuItems());
        var menuById = BuildMenuItems().ToDictionary(item => item.Id);

        foreach (var day in match.Days)
        {
            Assert.All(day.Items, item =>
            {
                var category = menuById[item.KitchenMenuItemId].Category;

                if (item.MealSlot == KitchenMealSlot.Breakfast)
                {
                    Assert.Equal(MenuItemCategory.Breakfast, category);
                }
                else if (item.MealSlot is KitchenMealSlot.MorningSnack or KitchenMealSlot.AfternoonSnack)
                {
                    Assert.Equal(MenuItemCategory.Snack, category);
                }
                else
                {
                    Assert.Equal(MenuItemCategory.MainMeal, category);
                }
            });
        }
    }

    [Fact]
    public void Match_DoesNotRepeatSameKitchenProductWithinOneDay()
    {
        var match = KitchenPlanMatcher.Match(BuildSubscription(), BuildMenuItems());

        foreach (var day in match.Days)
        {
            Assert.Equal(
                day.Items.Count,
                day.Items.Select(item => item.KitchenMenuItemId).Distinct().Count());
        }
    }

    [Fact]
    public void Match_FailsWhenRequiredMealSlotHasNoEligibleItems()
    {
        var subscription = BuildSubscription();
        var menuItems = BuildMenuItems()
            .Where(item => item.Category != MenuItemCategory.Snack)
            .ToList();

        var match = KitchenPlanMatcher.Match(subscription, menuItems);

        Assert.Equal(KitchenMealPlanStatus.Failed, match.Status);
        Assert.Empty(match.Days);
        Assert.Contains("Ara", match.Message);
    }

    [Fact]
    public void Match_ExcludesInactiveAndPlanIneligibleItems()
    {
        var subscription = BuildSubscription();
        var menuItems = BuildMenuItems();
        menuItems.Add(new KitchenMenuItem
        {
            Id = 99,
            Name = "Hidden Breakfast",
            Category = MenuItemCategory.Breakfast,
            Calories = 500,
            ProteinGrams = 40,
            CarbohydrateGrams = 45,
            FatGrams = 15,
            UnitPrice = 1,
            Ingredients = "Hidden",
            IsActive = true,
            IsPlanEligible = false
        });

        var match = KitchenPlanMatcher.Match(subscription, menuItems);

        Assert.DoesNotContain(
            match.Days.SelectMany(day => day.Items),
            item => item.KitchenMenuItemId == 99);
    }

    [Fact]
    public void Match_GeneratesPlanFromSeedMenuForEveryNutritionGoal()
    {
        foreach (var goal in Enum.GetValues<NutritionGoal>())
        {
            var subscription = BuildSubscription(goal: goal);
            var match = KitchenPlanMatcher.Match(subscription, KitchenMenuItemSeed.Defaults);

            Assert.True(
                match.Status == KitchenMealPlanStatus.Generated,
                $"{goal}: {match.Message}");
            Assert.All(match.Days, day => Assert.Equal(5, day.Items.Count));
        }
    }

    private static KitchenSubscription BuildSubscription(
        int dailyCalories = 1800,
        int proteinGrams = 130,
        int carbohydrateGrams = 180,
        int fatGrams = 60,
        NutritionGoal goal = NutritionGoal.FatLoss)
    {
        var startsOn = new DateOnly(2026, 7, 29);

        return new KitchenSubscription
        {
            Id = 1,
            Plan = KitchenSubscriptionPlan.FiveDays,
            Goal = goal,
            DailyCalories = dailyCalories,
            ProteinGrams = proteinGrams,
            CarbohydrateGrams = carbohydrateGrams,
            FatGrams = fatGrams,
            StartsOn = startsOn,
            EndsOn = startsOn.AddDays(4)
        };
    }

    private static List<KitchenMenuItem> BuildMenuItems()
    {
        return
        [
            new()
            {
                Id = 1,
                Name = "Lean Breakfast Plate",
                Category = MenuItemCategory.Breakfast,
                Calories = 360,
                ProteinGrams = 32,
                CarbohydrateGrams = 30,
                FatGrams = 12,
                UnitPrice = 245,
                Ingredients = "Egg whites, cheese, whole grain bread",
                Tags = "high protein",
                IsActive = true,
                IsPlanEligible = true
            },
            new()
            {
                Id = 4,
                Name = "Overnight Oat Protein Cup",
                Category = MenuItemCategory.Breakfast,
                Calories = 430,
                ProteinGrams = 28,
                CarbohydrateGrams = 54,
                FatGrams = 11,
                UnitPrice = 235,
                Ingredients = "Oats, yogurt, berries",
                Tags = "dengeli",
                IsActive = true,
                IsPlanEligible = true
            },
            new()
            {
                Id = 5,
                Name = "Performance Breakfast Wrap",
                Category = MenuItemCategory.Breakfast,
                Calories = 520,
                ProteinGrams = 36,
                CarbohydrateGrams = 48,
                FatGrams = 20,
                UnitPrice = 275,
                Ingredients = "Egg, turkey, wrap",
                Tags = "performans",
                IsActive = true,
                IsPlanEligible = true
            },
            new()
            {
                Id = 6,
                Name = "Vegan Morning Quinoa",
                Category = MenuItemCategory.Breakfast,
                Calories = 390,
                ProteinGrams = 18,
                CarbohydrateGrams = 58,
                FatGrams = 10,
                UnitPrice = 250,
                Ingredients = "Quinoa, almond milk, banana",
                Tags = "saglikli yasam",
                IsActive = true,
                IsPlanEligible = true
            },
            new()
            {
                Id = 2,
                Name = "Protein Power Bowl",
                Category = MenuItemCategory.MainMeal,
                Calories = 520,
                ProteinGrams = 42,
                CarbohydrateGrams = 48,
                FatGrams = 18,
                UnitPrice = 295,
                Ingredients = "Chicken, quinoa, greens",
                Tags = "high protein",
                IsActive = true,
                IsPlanEligible = true
            },
            new()
            {
                Id = 10,
                Name = "Lean Turkey Rice Box",
                Category = MenuItemCategory.MainMeal,
                Calories = 470,
                ProteinGrams = 44,
                CarbohydrateGrams = 50,
                FatGrams = 9,
                UnitPrice = 285,
                Ingredients = "Turkey, rice, vegetables",
                Tags = "dusuk kalori",
                IsActive = true,
                IsPlanEligible = true
            },
            new()
            {
                Id = 11,
                Name = "Salmon Performance Plate",
                Category = MenuItemCategory.MainMeal,
                Calories = 650,
                ProteinGrams = 46,
                CarbohydrateGrams = 58,
                FatGrams = 25,
                UnitPrice = 420,
                Ingredients = "Salmon, sweet potato, greens",
                Tags = "performans",
                IsActive = true,
                IsPlanEligible = true
            },
            new()
            {
                Id = 12,
                Name = "Beef Strength Bowl",
                Category = MenuItemCategory.MainMeal,
                Calories = 690,
                ProteinGrams = 52,
                CarbohydrateGrams = 62,
                FatGrams = 24,
                UnitPrice = 390,
                Ingredients = "Beef, bulgur, vegetables",
                Tags = "yuksek protein",
                IsActive = true,
                IsPlanEligible = true
            },
            new()
            {
                Id = 13,
                Name = "Mediterranean Chickpea Plate",
                Category = MenuItemCategory.MainMeal,
                Calories = 510,
                ProteinGrams = 24,
                CarbohydrateGrams = 66,
                FatGrams = 16,
                UnitPrice = 260,
                Ingredients = "Chickpeas, rice, vegetables",
                Tags = "vejetaryen",
                IsActive = true,
                IsPlanEligible = true
            },
            new()
            {
                Id = 14,
                Name = "Low Calorie Chicken Salad",
                Category = MenuItemCategory.MainMeal,
                Calories = 390,
                ProteinGrams = 40,
                CarbohydrateGrams = 24,
                FatGrams = 13,
                UnitPrice = 270,
                Ingredients = "Chicken, greens, dressing",
                Tags = "dusuk kalori",
                IsActive = true,
                IsPlanEligible = true
            },
            new()
            {
                Id = 15,
                Name = "Gluten Free Chicken Potato Box",
                Category = MenuItemCategory.MainMeal,
                Calories = 560,
                ProteinGrams = 43,
                CarbohydrateGrams = 60,
                FatGrams = 15,
                UnitPrice = 305,
                Ingredients = "Chicken, potato, vegetables",
                Tags = "glutensiz",
                IsActive = true,
                IsPlanEligible = true
            },
            new()
            {
                Id = 16,
                Name = "Tofu Veggie Noodle Bowl",
                Category = MenuItemCategory.MainMeal,
                Calories = 540,
                ProteinGrams = 30,
                CarbohydrateGrams = 68,
                FatGrams = 16,
                UnitPrice = 295,
                Ingredients = "Tofu, rice noodles, vegetables",
                Tags = "saglikli yasam",
                IsActive = true,
                IsPlanEligible = true
            },
            new()
            {
                Id = 3,
                Name = "Veggie Crunch Snack Box",
                Category = MenuItemCategory.Snack,
                Calories = 290,
                ProteinGrams = 21,
                CarbohydrateGrams = 26,
                FatGrams = 10,
                UnitPrice = 210,
                Ingredients = "Chickpeas, cheese, vegetables",
                Tags = "low calorie",
                IsActive = true,
                IsPlanEligible = true
            },
            new()
            {
                Id = 30,
                Name = "Greek Yogurt Protein Jar",
                Category = MenuItemCategory.Snack,
                Calories = 250,
                ProteinGrams = 30,
                CarbohydrateGrams = 22,
                FatGrams = 5,
                UnitPrice = 190,
                Ingredients = "Greek yogurt, whey, berries",
                Tags = "yuksek protein",
                IsActive = true,
                IsPlanEligible = true
            },
            new()
            {
                Id = 31,
                Name = "Rice Cake Peanut Stack",
                Category = MenuItemCategory.Snack,
                Calories = 310,
                ProteinGrams = 12,
                CarbohydrateGrams = 42,
                FatGrams = 11,
                UnitPrice = 175,
                Ingredients = "Rice cakes, peanut butter, banana",
                Tags = "performans",
                IsActive = true,
                IsPlanEligible = true
            },
            new()
            {
                Id = 32,
                Name = "Cottage Cheese Fruit Cup",
                Category = MenuItemCategory.Snack,
                Calories = 220,
                ProteinGrams = 24,
                CarbohydrateGrams = 20,
                FatGrams = 5,
                UnitPrice = 185,
                Ingredients = "Cottage cheese, fruit",
                Tags = "dengeli",
                IsActive = true,
                IsPlanEligible = true
            },
            new()
            {
                Id = 33,
                Name = "Hummus Veggie Cup",
                Category = MenuItemCategory.Snack,
                Calories = 260,
                ProteinGrams = 10,
                CarbohydrateGrams = 30,
                FatGrams = 11,
                UnitPrice = 170,
                Ingredients = "Hummus, vegetables",
                Tags = "saglikli yasam",
                IsActive = true,
                IsPlanEligible = true
            },
            new()
            {
                Id = 34,
                Name = "Protein Energy Bites",
                Category = MenuItemCategory.Snack,
                Calories = 330,
                ProteinGrams = 24,
                CarbohydrateGrams = 34,
                FatGrams = 11,
                UnitPrice = 195,
                Ingredients = "Oats, whey, cocoa",
                Tags = "yuksek protein",
                IsActive = true,
                IsPlanEligible = true
            }
        ];
    }
}
