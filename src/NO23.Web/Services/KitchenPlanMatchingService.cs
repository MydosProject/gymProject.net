using System.Globalization;
using Microsoft.EntityFrameworkCore;
using NO23.Web.Data;
using NO23.Web.Domain.Entities;
using NO23.Web.Domain.Enums;

namespace NO23.Web.Services;

public class KitchenPlanMatchingService(ApplicationDbContext dbContext)
{
    public const string CurrentCalculationVersion = "v1";

    public async Task<KitchenPlanGenerationResult> GenerateAsync(
        int kitchenSubscriptionId,
        CalorieCalculationRequest sourceRequest)
    {
        var existingPlan = await dbContext.KitchenMealPlans
            .AsNoTracking()
            .FirstOrDefaultAsync(plan => plan.KitchenSubscriptionId == kitchenSubscriptionId);

        if (existingPlan is not null)
        {
            return new KitchenPlanGenerationResult(
                existingPlan.Status == KitchenMealPlanStatus.Generated,
                existingPlan.Id,
                existingPlan.Status,
                null);
        }

        var subscription = await dbContext.KitchenSubscriptions
            .Include(item => item.MealSelections)
            .FirstOrDefaultAsync(item => item.Id == kitchenSubscriptionId);

        if (subscription is null)
        {
            return KitchenPlanGenerationResult.Fail("Kitchen aboneliği bulunamadı.");
        }

        var menuItems = await dbContext.KitchenMenuItems
            .AsNoTracking()
            .Where(item => item.IsActive && item.IsPlanEligible)
            .OrderBy(item => item.DisplayOrder)
            .ThenBy(item => item.Name)
            .ToListAsync();

        var match = KitchenPlanMatcher.Match(subscription, menuItems);

        if (match.Status == KitchenMealPlanStatus.Failed)
        {
            return KitchenPlanGenerationResult.Fail(
                match.Message ?? "Kitchen beslenme planı oluşturulamadı.");
        }

        var mealPlan = BuildMealPlan(subscription, sourceRequest, match);

        dbContext.KitchenMealPlans.Add(mealPlan);
        await dbContext.SaveChangesAsync();

        return new KitchenPlanGenerationResult(
            true,
            mealPlan.Id,
            KitchenMealPlanStatus.Generated,
            null);
    }

    private static KitchenMealPlan BuildMealPlan(
        KitchenSubscription subscription,
        CalorieCalculationRequest sourceRequest,
        KitchenPlanMatch match)
    {
        return new KitchenMealPlan
        {
            KitchenSubscriptionId = subscription.Id,
            Status = KitchenMealPlanStatus.Generated,
            CalculationVersion = CurrentCalculationVersion,
            SourceHeightCm = sourceRequest.HeightCm,
            SourceWeightKg = sourceRequest.WeightKg,
            SourceAge = sourceRequest.Age,
            SourceGender = sourceRequest.Gender,
            SourceActivityLevel = sourceRequest.ActivityLevel,
            SourceGoal = sourceRequest.Goal,
            TargetDailyCalories = subscription.DailyCalories,
            TargetProteinGrams = subscription.ProteinGrams,
            TargetCarbohydrateGrams = subscription.CarbohydrateGrams,
            TargetFatGrams = subscription.FatGrams,
            Days = match.Days
                .Select(day => new KitchenMealPlanDay
                {
                    DayNumber = day.DayNumber,
                    PlanDate = day.PlanDate,
                    TotalCalories = day.TotalCalories,
                    TotalProteinGrams = day.TotalProteinGrams,
                    TotalCarbohydrateGrams = day.TotalCarbohydrateGrams,
                    TotalFatGrams = day.TotalFatGrams,
                    Items = day.Items
                        .Select(item => new KitchenMealPlanItem
                        {
                            KitchenMenuItemId = item.KitchenMenuItemId,
                            MealSlot = item.MealSlot,
                            Quantity = item.Quantity,
                            ProductNameSnapshot = item.ProductNameSnapshot,
                            CaloriesSnapshot = item.CaloriesSnapshot,
                            ProteinGramsSnapshot = item.ProteinGramsSnapshot,
                            CarbohydrateGramsSnapshot = item.CarbohydrateGramsSnapshot,
                            FatGramsSnapshot = item.FatGramsSnapshot,
                            UnitPriceSnapshot = item.UnitPriceSnapshot
                        })
                        .ToList()
                })
                .ToList()
        };
    }
}

public record KitchenPlanGenerationResult(
    bool Succeeded,
    int? MealPlanId,
    KitchenMealPlanStatus Status,
    string? Message)
{
    public static KitchenPlanGenerationResult Fail(string message)
    {
        return new KitchenPlanGenerationResult(false, null, KitchenMealPlanStatus.Failed, message);
    }
}

public record KitchenPlanMatch(
    KitchenMealPlanStatus Status,
    string? Message,
    IReadOnlyList<KitchenPlanDayMatch> Days);

public record KitchenPlanDayMatch(
    int DayNumber,
    DateOnly PlanDate,
    int TotalCalories,
    decimal TotalProteinGrams,
    decimal TotalCarbohydrateGrams,
    decimal TotalFatGrams,
    IReadOnlyList<KitchenPlanItemMatch> Items);

public record KitchenPlanItemMatch(
    int KitchenMenuItemId,
    KitchenMealSlot MealSlot,
    int Quantity,
    string ProductNameSnapshot,
    int CaloriesSnapshot,
    decimal ProteinGramsSnapshot,
    decimal CarbohydrateGramsSnapshot,
    decimal FatGramsSnapshot,
    decimal UnitPriceSnapshot);

public static class KitchenPlanMatcher
{
    private const int MaxQuantityPerSlot = 3;
    private const int SlotCandidateLimit = 12;
    private static readonly CultureInfo TurkishCulture = CultureInfo.GetCultureInfo("tr-TR");

    public static KitchenPlanMatch Match(
        KitchenSubscription subscription,
        IReadOnlyList<KitchenMenuItem> menuItems)
    {
        var planDayCount = subscription.EndsOn.DayNumber - subscription.StartsOn.DayNumber + 1;

        if (planDayCount <= 0)
        {
            return new KitchenPlanMatch(
                KitchenMealPlanStatus.Failed,
                "Kitchen aboneliğinin tarih aralığı geçerli değil.",
                []);
        }

        var slots = GetSelectedSlots(subscription);

        if (slots.Length == 0)
        {
            return new KitchenPlanMatch(
                KitchenMealPlanStatus.Failed,
                "Kitchen planı için seçili öğün bulunamadı.",
                []);
        }

        var candidatesBySlot = new List<IReadOnlyList<MealCandidate>>();

        foreach (var slot in slots)
        {
            var category = MapSlotToCategory(slot);
            var slotCandidates = menuItems
                .Where(item => item.IsActive && item.IsPlanEligible && item.Category == category)
                .SelectMany(item => Enumerable
                    .Range(1, MaxQuantityPerSlot)
                    .Select(quantity => new MealCandidate(item, slot, quantity)))
                .OrderBy(candidate => GetSlotScore(candidate, subscription))
                .ThenBy(candidate => candidate.Item.DisplayOrder)
                .ThenBy(candidate => candidate.Item.Name)
                .Take(SlotCandidateLimit)
                .ToList();

            if (slotCandidates.Count == 0)
            {
                return new KitchenPlanMatch(
                    KitchenMealPlanStatus.Failed,
                    $"{GetSlotDisplayName(slot)} için plana uygun aktif Kitchen ürünü bulunamadı.",
                    []);
            }

            candidatesBySlot.Add(slotCandidates);
        }

        var days = new List<KitchenPlanDayMatch>();
        IReadOnlySet<string> previousMenuItemKeys = new HashSet<string>();

        for (var dayIndex = 0; dayIndex < planDayCount; dayIndex++)
        {
            var bestCombination = FindBestCombination(
                candidatesBySlot,
                subscription,
                previousMenuItemKeys);

            if (bestCombination is null)
            {
                return new KitchenPlanMatch(
                    KitchenMealPlanStatus.Failed,
                    "Seçilen öğünler için aynı gün içinde tekrar etmeyen yeterli Kitchen ürünü bulunamadı.",
                    []);
            }

            var day = BuildDayMatch(
                dayIndex + 1,
                subscription.StartsOn.AddDays(dayIndex),
                bestCombination);

            previousMenuItemKeys = bestCombination
                .Select(candidate => GetMenuItemKey(candidate.Item))
                .ToHashSet();

            days.Add(day);
        }

        return new KitchenPlanMatch(KitchenMealPlanStatus.Generated, null, days);
    }

    private static IReadOnlyList<MealCandidate>? FindBestCombination(
        IReadOnlyList<IReadOnlyList<MealCandidate>> candidatesBySlot,
        KitchenSubscription subscription,
        IReadOnlySet<string> previousMenuItemKeys)
    {
        return BuildCombinations(candidatesBySlot)
            .Where(HasUniqueMenuItems)
            .OrderBy(combination => GetDailyScore(combination, subscription, previousMenuItemKeys))
            .ThenBy(combination => combination.Sum(candidate => candidate.TotalCalories))
            .FirstOrDefault();
    }

    private static KitchenPlanDayMatch BuildDayMatch(
        int dayNumber,
        DateOnly planDate,
        IReadOnlyList<MealCandidate> candidates)
    {
        var items = candidates
            .OrderBy(candidate => candidate.Slot)
            .Select(candidate => new KitchenPlanItemMatch(
                candidate.Item.Id,
                candidate.Slot,
                candidate.Quantity,
                candidate.Item.Name,
                candidate.Item.Calories,
                candidate.Item.ProteinGrams,
                candidate.Item.CarbohydrateGrams,
                candidate.Item.FatGrams,
                candidate.Item.UnitPrice))
            .ToList();

        return new KitchenPlanDayMatch(
            dayNumber,
            planDate,
            candidates.Sum(candidate => candidate.TotalCalories),
            candidates.Sum(candidate => candidate.TotalProteinGrams),
            candidates.Sum(candidate => candidate.TotalCarbohydrateGrams),
            candidates.Sum(candidate => candidate.TotalFatGrams),
            items);
    }

    private static IEnumerable<IReadOnlyList<MealCandidate>> BuildCombinations(
        IReadOnlyList<IReadOnlyList<MealCandidate>> candidatesBySlot)
    {
        var buffer = new MealCandidate[candidatesBySlot.Count];

        foreach (var combination in Build(0))
        {
            yield return combination;
        }

        IEnumerable<IReadOnlyList<MealCandidate>> Build(int index)
        {
            if (index == candidatesBySlot.Count)
            {
                yield return buffer.ToArray();
                yield break;
            }

            foreach (var candidate in candidatesBySlot[index])
            {
                buffer[index] = candidate;

                foreach (var combination in Build(index + 1))
                {
                    yield return combination;
                }
            }
        }
    }

    private static bool HasUniqueMenuItems(IReadOnlyList<MealCandidate> candidates)
    {
        return candidates
            .Select(candidate => GetMenuItemKey(candidate.Item))
            .Distinct()
            .Count() == candidates.Count;
    }

    private static double GetDailyScore(
        IReadOnlyList<MealCandidate> candidates,
        KitchenSubscription subscription,
        IReadOnlySet<string> previousMenuItemKeys)
    {
        var calories = candidates.Sum(candidate => candidate.TotalCalories);
        var protein = candidates.Sum(candidate => candidate.TotalProteinGrams);
        var carbohydrate = candidates.Sum(candidate => candidate.TotalCarbohydrateGrams);
        var fat = candidates.Sum(candidate => candidate.TotalFatGrams);

        var selectedCalorieRatio = candidates
            .Select(candidate => candidate.Slot)
            .Distinct()
            .Sum(GetSlotCalorieRatio);

        var targetCalories = subscription.DailyCalories * selectedCalorieRatio;
        var targetProtein = subscription.ProteinGrams * selectedCalorieRatio;
        var targetCarbohydrate = subscription.CarbohydrateGrams * selectedCalorieRatio;
        var targetFat = subscription.FatGrams * selectedCalorieRatio;

        var calorieScore = (double)GetDifferenceRatio(calories, targetCalories) * 6;
        var proteinScore = GetMacroScore(protein, targetProtein, penalizeDeficit: true) * 3;
        var carbohydrateScore = GetMacroScore(carbohydrate, targetCarbohydrate, penalizeDeficit: false);
        var fatScore = GetMacroScore(fat, targetFat, penalizeDeficit: false);
        var repetitionPenalty = candidates.Count(candidate => previousMenuItemKeys.Contains(GetMenuItemKey(candidate.Item))) * 0.2;
        var goalBonus = candidates.Sum(candidate => GetGoalBonus(candidate.Item, subscription.Goal));

        return calorieScore + proteinScore + carbohydrateScore + fatScore + repetitionPenalty - goalBonus;
    }

    private static double GetSlotScore(MealCandidate candidate, KitchenSubscription subscription)
    {
        var slotCalorieTarget = subscription.DailyCalories * GetSlotCalorieRatio(candidate.Slot);
        var slotProteinTarget = subscription.ProteinGrams * GetSlotCalorieRatio(candidate.Slot);

        return (double)GetDifferenceRatio(candidate.TotalCalories, slotCalorieTarget)
            + GetMacroScore(candidate.TotalProteinGrams, slotProteinTarget, penalizeDeficit: true);
    }

    private static double GetMacroScore(decimal actual, decimal target, bool penalizeDeficit)
    {
        if (target <= 0)
        {
            return 0;
        }

        if (actual < target)
        {
            var deficit = (double)((target - actual) / target);
            return penalizeDeficit ? deficit * 1.5 : deficit;
        }

        return (double)((actual - target) / target) * 0.35;
    }

    private static decimal GetDifferenceRatio(decimal actual, decimal target)
    {
        return target <= 0 ? 0 : Math.Abs(actual - target) / target;
    }

    private static decimal GetSlotCalorieRatio(KitchenMealSlot slot)
    {
        return slot switch
        {
            KitchenMealSlot.Breakfast => 0.20m,
            KitchenMealSlot.MorningSnack => 0.10m,
            KitchenMealSlot.Lunch => 0.30m,
            KitchenMealSlot.AfternoonSnack => 0.10m,
            KitchenMealSlot.Dinner => 0.30m,
            _ => throw new ArgumentOutOfRangeException(nameof(slot), slot, null)
        };
    }

    private static KitchenMealSlot[] GetSelectedSlots(KitchenSubscription subscription)
    {
        if (subscription.MealSelections.Count == 0)
        {
            return GetSlots(subscription.Plan);
        }

        return subscription.MealSelections
            .Select(selection => selection.MealSlot)
            .Distinct()
            .OrderBy(slot => slot)
            .ToArray();
    }

    private static KitchenMealSlot[] GetSlots(KitchenSubscriptionPlan plan)
    {
        return plan switch
        {
            KitchenSubscriptionPlan.FiveDays =>
            [
                KitchenMealSlot.Breakfast,
                KitchenMealSlot.MorningSnack,
                KitchenMealSlot.Lunch,
                KitchenMealSlot.AfternoonSnack,
                KitchenMealSlot.Dinner
            ],
            KitchenSubscriptionPlan.TenDays =>
            [
                KitchenMealSlot.Breakfast,
                KitchenMealSlot.MorningSnack,
                KitchenMealSlot.Lunch,
                KitchenMealSlot.AfternoonSnack,
                KitchenMealSlot.Dinner
            ],
            KitchenSubscriptionPlan.TwentyDays =>
            [
                KitchenMealSlot.Breakfast,
                KitchenMealSlot.MorningSnack,
                KitchenMealSlot.Lunch,
                KitchenMealSlot.AfternoonSnack,
                KitchenMealSlot.Dinner
            ],
            KitchenSubscriptionPlan.Monthly =>
            [
                KitchenMealSlot.Breakfast,
                KitchenMealSlot.MorningSnack,
                KitchenMealSlot.Lunch,
                KitchenMealSlot.AfternoonSnack,
                KitchenMealSlot.Dinner
            ],
            _ => throw new ArgumentOutOfRangeException(nameof(plan), plan, null)
        };
    }

    private static MenuItemCategory MapSlotToCategory(KitchenMealSlot slot)
    {
        return slot switch
        {
            KitchenMealSlot.Breakfast => MenuItemCategory.Breakfast,
            KitchenMealSlot.MorningSnack => MenuItemCategory.Snack,
            KitchenMealSlot.Lunch => MenuItemCategory.MainMeal,
            KitchenMealSlot.AfternoonSnack => MenuItemCategory.Snack,
            KitchenMealSlot.Dinner => MenuItemCategory.MainMeal,
            _ => throw new ArgumentOutOfRangeException(nameof(slot), slot, null)
        };
    }

    private static string GetSlotDisplayName(KitchenMealSlot slot)
    {
        return slot switch
        {
            KitchenMealSlot.Breakfast => "Kahvaltı",
            KitchenMealSlot.MorningSnack => "Ara Öğün 1",
            KitchenMealSlot.Lunch => "Öğle Yemeği",
            KitchenMealSlot.AfternoonSnack => "Ara Öğün 2",
            KitchenMealSlot.Dinner => "Akşam Yemeği",
            _ => slot.ToString()
        };
    }

    private static double GetGoalBonus(KitchenMenuItem item, NutritionGoal goal)
    {
        var tags = (item.Tags ?? string.Empty).ToLower(TurkishCulture);

        return goal switch
        {
            NutritionGoal.FatLoss when
                tags.Contains("düşük kalori", StringComparison.Ordinal) ||
                tags.Contains("dusuk kalori", StringComparison.Ordinal) => 0.15,
            NutritionGoal.MuscleGain when
                tags.Contains("yüksek protein", StringComparison.Ordinal) ||
                tags.Contains("yuksek protein", StringComparison.Ordinal) => 0.15,
            NutritionGoal.PerformanceNutrition when tags.Contains("performans", StringComparison.Ordinal) => 0.15,
            NutritionGoal.WeightMaintenance when tags.Contains("dengeli", StringComparison.Ordinal) => 0.10,
            NutritionGoal.HealthyLifestyle when
                tags.Contains("sağlıklı yaşam", StringComparison.Ordinal) ||
                tags.Contains("saglikli yasam", StringComparison.Ordinal) => 0.10,
            NutritionGoal.HealthyLifestyle when tags.Contains("vejetaryen", StringComparison.Ordinal) => 0.05,
            _ => 0
        };
    }

    private static string GetMenuItemKey(KitchenMenuItem item)
    {
        return item.Id > 0
            ? item.Id.ToString(CultureInfo.InvariantCulture)
            : item.Name;
    }

    private sealed record MealCandidate(
        KitchenMenuItem Item,
        KitchenMealSlot Slot,
        int Quantity)
    {
        public int TotalCalories => Item.Calories * Quantity;

        public decimal TotalProteinGrams => Item.ProteinGrams * Quantity;

        public decimal TotalCarbohydrateGrams => Item.CarbohydrateGrams * Quantity;

        public decimal TotalFatGrams => Item.FatGrams * Quantity;
    }
}
