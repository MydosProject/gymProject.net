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
        var subscriptionPackage = await dbContext.KitchenSubscriptionPackages
            .AsNoTracking()
            .FirstOrDefaultAsync(package =>
                package.Plan == plan &&
                package.IsActive);

        if (subscriptionPackage is null)
        {
            TempData["ErrorMessage"] = "Seçilen Kitchen paketi şu anda aktif değil.";
            return RedirectToAction(nameof(Index));
        }

        if (subscriptionPackage.Days <= 0)
        {
            TempData["ErrorMessage"] = "Seçilen Kitchen paketinin gün sayısı geçerli değil.";
            return RedirectToAction(nameof(Index));
        }

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

        await using var transaction = await dbContext.Database.BeginTransactionAsync();

        var subscription = new KitchenSubscription
        {
            MemberProfileId = profile.Id,
            KitchenSubscriptionPackageId = subscriptionPackage.Id,
            Plan = subscriptionPackage.Plan,
            PackageNameSnapshot = subscriptionPackage.Name,
            PackagePriceSnapshot = subscriptionPackage.UnitPrice,
            PackageDaysSnapshot = subscriptionPackage.Days,
            Goal = input.Goal,
            DailyCalories = result.DailyCalories,
            ProteinGrams = result.ProteinGrams,
            CarbohydrateGrams = result.CarbohydrateGrams,
            FatGrams = result.FatGrams,
            StartsOn = startsOn,
            EndsOn = startsOn.AddDays(subscriptionPackage.Days - 1)
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
                "Kitchen beslenme planı oluşturulamadı.";

            return RedirectToAction(nameof(Index));
        }

        await transaction.CommitAsync();

        TempData["SuccessMessage"] =
            "Kitchen aboneliğin ve 5 öğünlük beslenme planın oluşturuldu.";

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SkipMeal(int mealPlanItemId)
    {
        return await SetMealSkippedAsync(
            mealPlanItemId,
            isSkipped: true,
            successMessage: "Öğün pas geçildi. Plan yenilendiğinde bu öğün üretim ihtiyacına dahil edilmez.");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RestoreMeal(int mealPlanItemId)
    {
        return await SetMealSkippedAsync(
            mealPlanItemId,
            isSkipped: false,
            successMessage: "Öğün yeniden plana alındı. Üretim planını güncellemek için admin tarafında Planı Yenile kullanılmalı.");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SkipDay(int mealPlanDayId)
    {
        return await SetDaySkippedAsync(
            mealPlanDayId,
            isSkipped: true,
            successMessage: "Gün pas geçildi. Bu güne ait aktif öğünler üretim ihtiyacına dahil edilmez.");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RestoreDay(int mealPlanDayId)
    {
        return await SetDaySkippedAsync(
            mealPlanDayId,
            isSkipped: false,
            successMessage: "Gün yeniden plana alındı. Üretim planını güncellemek için admin tarafında Planı Yenile kullanılmalı.");
    }

    private async Task<IActionResult> SetMealSkippedAsync(
        int mealPlanItemId,
        bool isSkipped,
        string successMessage)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Challenge();
        }

        var meal = await dbContext.KitchenMealPlanItems
            .Include(item => item.KitchenMealPlanDay)
            .ThenInclude(day => day.KitchenMealPlan)
            .ThenInclude(plan => plan.KitchenSubscription)
            .ThenInclude(subscription => subscription.MemberProfile)
            .FirstOrDefaultAsync(item => item.Id == mealPlanItemId);

        if (meal is null ||
            meal.KitchenMealPlanDay.KitchenMealPlan.KitchenSubscription.MemberProfile
                .ApplicationUserId != userId)
        {
            return NotFound();
        }

        var result = ValidateMealPlanChange(meal.KitchenMealPlanDay);

        if (!result.Succeeded)
        {
            TempData["ErrorMessage"] = result.Message;
            return RedirectToAction(nameof(Index), null, "nutrition-plan");
        }

        if (meal.IsSkipped != isSkipped)
        {
            meal.IsSkipped = isSkipped;
            meal.SkippedAtUtc = isSkipped ? DateTime.UtcNow : null;
            await dbContext.SaveChangesAsync();
        }

        TempData["SuccessMessage"] = successMessage;

        return RedirectToAction(nameof(Index), null, "nutrition-plan");
    }

    private async Task<IActionResult> SetDaySkippedAsync(
        int mealPlanDayId,
        bool isSkipped,
        string successMessage)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Challenge();
        }

        var day = await dbContext.KitchenMealPlanDays
            .Include(item => item.Items)
            .Include(item => item.KitchenMealPlan)
            .ThenInclude(plan => plan.KitchenSubscription)
            .ThenInclude(subscription => subscription.MemberProfile)
            .FirstOrDefaultAsync(item => item.Id == mealPlanDayId);

        if (day is null ||
            day.KitchenMealPlan.KitchenSubscription.MemberProfile.ApplicationUserId != userId)
        {
            return NotFound();
        }

        var result = ValidateMealPlanChange(day);

        if (!result.Succeeded)
        {
            TempData["ErrorMessage"] = result.Message;
            return RedirectToAction(nameof(Index), null, "nutrition-plan");
        }

        var changedAtUtc = DateTime.UtcNow;

        foreach (var meal in day.Items)
        {
            if (meal.IsSkipped == isSkipped)
            {
                continue;
            }

            meal.IsSkipped = isSkipped;
            meal.SkippedAtUtc = isSkipped ? changedAtUtc : null;
        }

        await dbContext.SaveChangesAsync();

        TempData["SuccessMessage"] = successMessage;

        return RedirectToAction(nameof(Index), null, "nutrition-plan");
    }

    private static KitchenMealPlanChangeResult ValidateMealPlanChange(KitchenMealPlanDay day)
    {
        var subscription = day.KitchenMealPlan.KitchenSubscription;
        var today = DateOnly.FromDateTime(DateTime.Today);

        if (subscription.Status != KitchenSubscriptionStatus.Active)
        {
            return KitchenMealPlanChangeResult.Fail(
                "Sadece aktif Kitchen aboneliğindeki öğünler değiştirilebilir.");
        }

        if (day.PlanDate <= today)
        {
            return KitchenMealPlanChangeResult.Fail(
                "Bugün veya geçmiş tarihli öğünler pas geçilemez. Sadece yarın ve sonrası için değişiklik yapabilirsin.");
        }

        return KitchenMealPlanChangeResult.Ok();
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
                    item.PackageNameSnapshot,
                    item.PackagePriceSnapshot,
                    item.PackageDaysSnapshot,
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
                    PackageName = subscription.PackageNameSnapshot,
                    PackagePrice = subscription.PackagePriceSnapshot,
                    PackageDays = subscription.PackageDaysSnapshot,
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
            SubscriptionPlans = await BuildSubscriptionPackagesAsync()
        };
    }

    private async Task<IReadOnlyList<KitchenSubscriptionPlanViewModel>> BuildSubscriptionPackagesAsync()
    {
        return await dbContext.KitchenSubscriptionPackages
            .AsNoTracking()
            .Where(package => package.IsActive)
            .OrderBy(package => package.DisplayOrder)
            .ThenBy(package => package.Name)
            .Select(package => new KitchenSubscriptionPlanViewModel
            {
                Plan = package.Plan,
                Name = package.Name,
                Description = package.Description,
                Days = package.Days,
                UnitPrice = package.UnitPrice,
                IsActive = package.IsActive
            })
            .ToListAsync();
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
                item.UnitPriceSnapshot,
                item.IsSkipped
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
                            TotalPrice = item.UnitPriceSnapshot * item.Quantity,
                            IsSkipped = item.IsSkipped,
                            CanSkip = day.PlanDate > DateOnly.FromDateTime(DateTime.Today)
                        })
                        .ToList();
                    var activeMeals = meals
                        .Where(meal => !meal.IsSkipped)
                        .ToList();

                    return new KitchenMealPlanDayViewModel
                    {
                        Id = day.Id,
                        DayNumber = day.DayNumber,
                        PlanDate = day.PlanDate,
                        TotalCalories = activeMeals.Sum(meal => meal.Calories),
                        TotalProteinGrams = activeMeals.Sum(meal => meal.ProteinGrams),
                        TotalCarbohydrateGrams = activeMeals.Sum(meal => meal.CarbohydrateGrams),
                        TotalFatGrams = activeMeals.Sum(meal => meal.FatGrams),
                        TotalPrice = activeMeals.Sum(meal => meal.TotalPrice),
                        CanSkip = day.PlanDate > DateOnly.FromDateTime(DateTime.Today),
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

public record KitchenMealPlanChangeResult(
    bool Succeeded,
    string? Message)
{
    public static KitchenMealPlanChangeResult Ok()
    {
        return new KitchenMealPlanChangeResult(true, null);
    }

    public static KitchenMealPlanChangeResult Fail(string message)
    {
        return new KitchenMealPlanChangeResult(false, message);
    }
}
