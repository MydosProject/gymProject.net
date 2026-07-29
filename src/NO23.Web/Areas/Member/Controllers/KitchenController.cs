using System.Security.Claims;
using System.Globalization;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NO23.Web.Data;
using NO23.Web.Data.Seed;
using NO23.Web.Domain.Entities;
using NO23.Web.Domain.Enums;
using NO23.Web.Services;
using NO23.Web.ViewModels.Member;

namespace NO23.Web.Areas.Member.Controllers;

[Area("Member")]
[Authorize(Roles = ApplicationRoles.Member)]
public class KitchenController(
    ApplicationDbContext dbContext,
    CalorieCalculatorService calorieCalculator,
    KitchenPlanMatchingService kitchenPlanMatchingService) : Controller
{
    private const string CalculatorInputSessionKey = "NO23.Kitchen.CalculatorInput";
    private const string CalculatorResultSessionKey = "NO23.Kitchen.CalculatorResult";
    private static readonly CultureInfo TurkishCulture = CultureInfo.GetCultureInfo("tr-TR");
    private static readonly StringComparer TurkishIgnoreCaseComparer =
        StringComparer.Create(TurkishCulture, ignoreCase: true);
    private static readonly string[] PreferredTagOrder =
    [
        "yüksek protein",
        "düşük kalori",
        "glutensiz",
        "vejetaryen"
    ];

    public async Task<IActionResult> Index()
    {
        return View(await BuildDashboardAsync(
            GetStoredCalculatorInput() ?? new CalorieCalculatorInputViewModel(),
            GetStoredCalculatorResult()));
    }

    [HttpGet]
    public IActionResult Calculator()
    {
        return LocalRedirect($"{Url.Action(nameof(Index))}#calculator");
    }

    [HttpGet]
    public async Task<IActionResult> Menu()
    {
        return View(await BuildMenuDashboardAsync());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Calculate(
        [Bind(Prefix = "CalculatorInput")] CalorieCalculatorInputViewModel input)
    {
        if (!ModelState.IsValid)
        {
            return View(
                "Index",
                await BuildDashboardAsync(input, null));
        }

        var calculationRequest = new CalorieCalculationRequest
        {
            HeightCm = input.HeightCm,
            WeightKg = input.WeightKg,
            Age = input.Age,
            Gender = input.Gender,
            ActivityLevel = input.ActivityLevel,
            Goal = input.Goal
        };

        var result = calorieCalculator.Calculate(calculationRequest);

        var recommendation = new CalorieRecommendationViewModel
        {
            Goal = input.Goal,
            DailyCalories = result.DailyCalories,
            ProteinGrams = result.ProteinGrams,
            CarbohydrateGrams = result.CarbohydrateGrams,
            FatGrams = result.FatGrams
        };

        HttpContext.Session.SetString(
            CalculatorInputSessionKey,
            JsonSerializer.Serialize(input));
        HttpContext.Session.SetString(
            CalculatorResultSessionKey,
            JsonSerializer.Serialize(recommendation));

        return LocalRedirect($"{Url.Action(nameof(Index))}#calculator");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Subscribe(
        KitchenSubscriptionPlan plan,
        CalorieCalculatorInputViewModel input)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Challenge();
        }

        if (!ModelState.IsValid)
        {
            TempData["ErrorMessage"] = "Abonelik için önce geçerli kalori bilgilerini girmelisin.";
            return View("Index", await BuildDashboardAsync(input, null));
        }

        var profile = await dbContext.MemberProfiles
            .FirstOrDefaultAsync(member => member.ApplicationUserId == userId);

        if (profile is null)
        {
            TempData["ErrorMessage"] = "Üye profili bulunamadı.";
            return RedirectToAction(nameof(Index));
        }

        var calculationRequest = new CalorieCalculationRequest
        {
            HeightCm = input.HeightCm,
            WeightKg = input.WeightKg,
            Age = input.Age,
            Gender = input.Gender,
            ActivityLevel = input.ActivityLevel,
            Goal = input.Goal
        };

        var result = calorieCalculator.Calculate(calculationRequest);

        var today = DateOnly.FromDateTime(DateTime.Today);
        var hasActiveSubscription = await dbContext.KitchenSubscriptions
            .AnyAsync(subscription =>
                subscription.MemberProfileId == profile.Id &&
                subscription.Status == KitchenSubscriptionStatus.Active &&
                subscription.EndsOn >= today);

        if (hasActiveSubscription)
        {
            TempData["ErrorMessage"] = "Aktif Kitchen aboneliğin devam ediyor. Yeni abonelik oluşturmadan önce mevcut aboneliğini tamamlamalısın.";
            return RedirectToAction(nameof(Index));
        }

        var startsOn = DateOnly.FromDateTime(DateTime.Today.AddDays(1));
        var days = GetPlanDays(plan);

        await using var transaction = await dbContext.Database.BeginTransactionAsync();

        var subscription = new KitchenSubscription
        {
            MemberProfileId = profile.Id,
            Plan = plan,
            Goal = input.Goal,
            DailyCalories = result.DailyCalories,
            ProteinGrams = result.ProteinGrams,
            CarbohydrateGrams = result.CarbohydrateGrams,
            FatGrams = result.FatGrams,
            StartsOn = startsOn,
            EndsOn = startsOn.AddDays(days - 1)
        };

        dbContext.KitchenSubscriptions.Add(subscription);
        await dbContext.SaveChangesAsync();

        var planResult = await kitchenPlanMatchingService.GenerateAsync(
            subscription.Id,
            calculationRequest);

        if (!planResult.Succeeded)
        {
            await transaction.RollbackAsync();

            TempData["ErrorMessage"] =
                planResult.Message ??
                "Kitchen beslenme plani olusturulamadi.";

            return RedirectToAction(nameof(Index));
        }

        await transaction.CommitAsync();

        TempData["SuccessMessage"] =
            "Kitchen aboneligin ve 5 ogunluk beslenme planin olusturuldu.";

        return RedirectToAction(nameof(Index));
    }

    private async Task<KitchenDashboardViewModel> BuildDashboardAsync(
        CalorieCalculatorInputViewModel input,
        CalorieRecommendationViewModel? recommendation)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var today = DateOnly.FromDateTime(DateTime.Today);

        ActiveKitchenSubscriptionViewModel? activeSubscription = null;

        if (!string.IsNullOrWhiteSpace(userId))
        {
            var subscription = await dbContext.KitchenSubscriptions
                .AsNoTracking()
                .Where(item =>
                    item.MemberProfile.ApplicationUserId == userId &&
                    item.Status == KitchenSubscriptionStatus.Active &&
                    item.EndsOn >= today)
                .OrderByDescending(item => item.CreatedAtUtc)
                .ThenByDescending(item => item.Id)
                .Select(item => new
                {
                    item.Id,
                    item.Plan,
                    item.Status,
                    item.Goal,
                    item.DailyCalories,
                    item.ProteinGrams,
                    item.CarbohydrateGrams,
                    item.FatGrams,
                    item.StartsOn,
                    item.EndsOn
                })
                .FirstOrDefaultAsync();

            if (subscription is not null)
            {
                activeSubscription = new ActiveKitchenSubscriptionViewModel
                {
                    Id = subscription.Id,
                    Plan = subscription.Plan.ToString(),
                    Status = subscription.Status.ToString(),
                    Goal = subscription.Goal.ToString(),
                    DailyCalories = subscription.DailyCalories,
                    ProteinGrams = subscription.ProteinGrams,
                    CarbohydrateGrams = subscription.CarbohydrateGrams,
                    FatGrams = subscription.FatGrams,
                    StartsOn = subscription.StartsOn,
                    EndsOn = subscription.EndsOn,
                    RemainingDays = Math.Max(0, subscription.EndsOn.DayNumber - today.DayNumber + 1),
                    MealPlan = await BuildMealPlanAsync(subscription.Id)
                };
            }
        }

        return new KitchenDashboardViewModel
        {
            CalculatorInput = input,
            Recommendation = recommendation,
            ActiveSubscription = activeSubscription,
            SubscriptionPlans =
            [
                new() { Plan = KitchenSubscriptionPlan.FiveDays, Name = "5 Günlük", Days = 5 },
                new() { Plan = KitchenSubscriptionPlan.TenDays, Name = "10 Günlük", Days = 10 },
                new() { Plan = KitchenSubscriptionPlan.TwentyDays, Name = "20 Günlük", Days = 20 },
                new() { Plan = KitchenSubscriptionPlan.Monthly, Name = "Aylık", Days = 30 }
            ]
        };
    }

    private async Task<KitchenMealPlanViewModel?> BuildMealPlanAsync(int kitchenSubscriptionId)
    {
        var mealPlan = await dbContext.KitchenMealPlans
            .AsNoTracking()
            .Where(plan => plan.KitchenSubscriptionId == kitchenSubscriptionId)
            .OrderByDescending(plan => plan.GeneratedAtUtc)
            .ThenByDescending(plan => plan.Id)
            .Select(plan => new
            {
                plan.Id,
                plan.Status,
                plan.GeneratedAtUtc
            })
            .FirstOrDefaultAsync();

        if (mealPlan is null)
        {
            return null;
        }

        var days = await dbContext.KitchenMealPlanDays
            .AsNoTracking()
            .Where(day => day.KitchenMealPlanId == mealPlan.Id)
            .OrderBy(day => day.DayNumber)
            .Select(day => new
            {
                day.Id,
                day.DayNumber,
                day.PlanDate,
                day.TotalCalories,
                day.TotalProteinGrams,
                day.TotalCarbohydrateGrams,
                day.TotalFatGrams
            })
            .ToListAsync();

        var dayIds = days
            .Select(day => day.Id)
            .ToList();

        var items = await dbContext.KitchenMealPlanItems
            .AsNoTracking()
            .Where(item => dayIds.Contains(item.KitchenMealPlanDayId))
            .OrderBy(item => item.KitchenMealPlanDayId)
            .ThenBy(item => item.MealSlot)
            .ThenBy(item => item.Id)
            .Select(item => new
            {
                item.Id,
                item.KitchenMealPlanDayId,
                item.KitchenMenuItemId,
                item.MealSlot,
                item.Quantity,
                item.ProductNameSnapshot,
                item.CaloriesSnapshot,
                item.ProteinGramsSnapshot,
                item.CarbohydrateGramsSnapshot,
                item.FatGramsSnapshot,
                item.UnitPriceSnapshot
            })
            .ToListAsync();

        var itemsByDayId = items
            .GroupBy(item => item.KitchenMealPlanDayId)
            .ToDictionary(group => group.Key, group => group.ToList());

        return new KitchenMealPlanViewModel
        {
            Id = mealPlan.Id,
            Status = mealPlan.Status.ToString(),
            GeneratedAtUtc = mealPlan.GeneratedAtUtc,
            Days = days
                .Select(day =>
                {
                    var dayMeals = itemsByDayId.TryGetValue(day.Id, out var dayItems)
                        ? dayItems
                        : [];

                    var meals = dayMeals
                        .Select(item => new KitchenMealPlanMealViewModel
                        {
                            Id = item.Id,
                            KitchenMenuItemId = item.KitchenMenuItemId,
                            MealSlot = item.MealSlot.ToString(),
                            MealSlotDisplayName = GetMealSlotDisplayName(item.MealSlot),
                            Quantity = item.Quantity,
                            ProductName = item.ProductNameSnapshot,
                            Calories = item.CaloriesSnapshot * item.Quantity,
                            ProteinGrams = item.ProteinGramsSnapshot * item.Quantity,
                            CarbohydrateGrams = item.CarbohydrateGramsSnapshot * item.Quantity,
                            FatGrams = item.FatGramsSnapshot * item.Quantity,
                            UnitPrice = item.UnitPriceSnapshot,
                            TotalPrice = item.UnitPriceSnapshot * item.Quantity
                        })
                        .ToList();

                    return new KitchenMealPlanDayViewModel
                    {
                        Id = day.Id,
                        DayNumber = day.DayNumber,
                        PlanDate = day.PlanDate,
                        TotalCalories = day.TotalCalories,
                        TotalProteinGrams = day.TotalProteinGrams,
                        TotalCarbohydrateGrams = day.TotalCarbohydrateGrams,
                        TotalFatGrams = day.TotalFatGrams,
                        TotalPrice = meals.Sum(meal => meal.TotalPrice),
                        Meals = meals
                    };
                })
                .ToList()
        };
    }

    private static string GetMealSlotDisplayName(KitchenMealSlot slot)
    {
        return slot switch
        {
            KitchenMealSlot.Breakfast => "Kahvalt\u0131",
            KitchenMealSlot.MorningSnack => "1. Ara \u00d6\u011f\u00fcn",
            KitchenMealSlot.Lunch => "\u00d6\u011fle Yeme\u011fi",
            KitchenMealSlot.AfternoonSnack => "2. Ara \u00d6\u011f\u00fcn",
            KitchenMealSlot.Dinner => "Ak\u015fam Yeme\u011fi",
            _ => slot.ToString()
        };
    }

    private async Task<KitchenDashboardViewModel> BuildMenuDashboardAsync()
    {
        var rawItems = await dbContext.KitchenMenuItems
            .AsNoTracking()
            .Where(item => item.IsActive)
            .OrderBy(item => item.DisplayOrder)
            .ThenBy(item => item.Name)
            .Select(item => new
            {
                item.Id,
                item.Name,
                Category = item.Category.ToString(),
                item.Calories,
                item.UnitPrice,
                item.ProteinGrams,
                item.CarbohydrateGrams,
                item.FatGrams,
                item.Ingredients,
                item.Allergens,
                item.Tags
            })
            .ToListAsync();

        var menuItems = rawItems
            .Select(item =>
            {
                var tagList = SplitTags(item.Tags);

                return new KitchenMenuItemCardViewModel
                {
                    Id = item.Id,
                    Name = item.Name,
                    Category = item.Category,
                    Calories = item.Calories,
                    UnitPrice = item.UnitPrice,
                    ProteinGrams = item.ProteinGrams,
                    CarbohydrateGrams = item.CarbohydrateGrams,
                    FatGrams = item.FatGrams,
                    Ingredients = item.Ingredients,
                    Allergens = item.Allergens,
                    Tags = string.Join(", ", tagList),
                    TagList = tagList
                };
            })
            .ToList();

        return new KitchenDashboardViewModel
        {
            MenuItems = menuItems,
            CategoryFilters = BuildCategoryFilters(menuItems),
            TagFilters = BuildTagFilters(menuItems)
        };
    }

    private static IReadOnlyList<KitchenFilterOptionViewModel> BuildCategoryFilters(
        IReadOnlyList<KitchenMenuItemCardViewModel> menuItems)
    {
        return Enum.GetValues<MenuItemCategory>()
            .Select(category =>
            {
                var value = category.ToString();

                return new KitchenFilterOptionViewModel
                {
                    Value = value,
                    Label = GetCategoryPluralName(category),
                    ItemCount = menuItems.Count(item => item.Category == value)
                };
            })
            .Where(filter => filter.ItemCount > 0)
            .ToList();
    }

    private static IReadOnlyList<KitchenFilterOptionViewModel> BuildTagFilters(
        IReadOnlyList<KitchenMenuItemCardViewModel> menuItems)
    {
        var tagCounts = menuItems
            .SelectMany(item => item.TagList.Distinct(TurkishIgnoreCaseComparer))
            .GroupBy(tag => tag, TurkishIgnoreCaseComparer)
            .ToDictionary(
                group => group.Key,
                group => group.Count(),
                TurkishIgnoreCaseComparer);

        return tagCounts
            .Select(item => new KitchenFilterOptionViewModel
            {
                Value = NormalizeTag(item.Key),
                Label = ToTitleCase(item.Key),
                ItemCount = item.Value
            })
            .OrderBy(filter =>
            {
                var index = Array.IndexOf(PreferredTagOrder, filter.Value);
                return index < 0 ? PreferredTagOrder.Length : index;
            })
            .ThenBy(filter => filter.Label, TurkishIgnoreCaseComparer)
            .ToList();
    }

    private static IReadOnlyList<string> SplitTags(string? tags)
    {
        if (string.IsNullOrWhiteSpace(tags))
        {
            return [];
        }

        return tags
            .Split(
                [',', ';', '|'],
                StringSplitOptions.RemoveEmptyEntries |
                StringSplitOptions.TrimEntries)
            .Select(NormalizeTag)
            .Where(tag => !string.IsNullOrWhiteSpace(tag))
            .Distinct(TurkishIgnoreCaseComparer)
            .ToList();
    }

    private static string NormalizeTag(string value)
    {
        return value
            .Trim()
            .ToLower(TurkishCulture);
    }

    private static string ToTitleCase(string value)
    {
        return TurkishCulture.TextInfo.ToTitleCase(value);
    }

    private static string GetCategoryPluralName(MenuItemCategory category)
    {
        return category switch
        {
            MenuItemCategory.Breakfast => "Kahvaltılar",
            MenuItemCategory.MainMeal => "Ana Öğünler",
            MenuItemCategory.Snack => "Ara Öğünler",
            MenuItemCategory.Dessert => "Tatlılar",
            MenuItemCategory.Beverage => "İçecekler",
            _ => category.ToString()
        };
    }

    private static int GetPlanDays(KitchenSubscriptionPlan plan)
    {
        return plan switch
        {
            KitchenSubscriptionPlan.FiveDays => 5,
            KitchenSubscriptionPlan.TenDays => 10,
            KitchenSubscriptionPlan.TwentyDays => 20,
            KitchenSubscriptionPlan.Monthly => 30,
            _ => throw new ArgumentOutOfRangeException(nameof(plan), plan, null)
        };
    }

    private CalorieCalculatorInputViewModel? GetStoredCalculatorInput()
    {
        return GetSessionValue<CalorieCalculatorInputViewModel>(
            CalculatorInputSessionKey);
    }

    private CalorieRecommendationViewModel? GetStoredCalculatorResult()
    {
        return GetSessionValue<CalorieRecommendationViewModel>(
            CalculatorResultSessionKey);
    }

    private T? GetSessionValue<T>(string key)
    {
        var value = HttpContext.Session.GetString(key);

        if (string.IsNullOrWhiteSpace(value))
        {
            return default;
        }

        try
        {
            return JsonSerializer.Deserialize<T>(value);
        }
        catch (JsonException)
        {
            HttpContext.Session.Remove(key);
            return default;
        }
    }
}
