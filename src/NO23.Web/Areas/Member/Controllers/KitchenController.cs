using System.Security.Claims;
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
    CalorieCalculatorService calorieCalculator) : Controller
{
    private const string CalculatorInputSessionKey = "NO23.Kitchen.CalculatorInput";
    private const string CalculatorResultSessionKey = "NO23.Kitchen.CalculatorResult";

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
        return View(new KitchenDashboardViewModel
        {
            MenuItems = await BuildMenuItemsAsync()
        });
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

        var result = calorieCalculator.Calculate(new CalorieCalculationRequest
        {
            HeightCm = input.HeightCm,
            WeightKg = input.WeightKg,
            Age = input.Age,
            Gender = input.Gender,
            ActivityLevel = input.ActivityLevel,
            Goal = input.Goal
        });

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

        var result = calorieCalculator.Calculate(new CalorieCalculationRequest
        {
            HeightCm = input.HeightCm,
            WeightKg = input.WeightKg,
            Age = input.Age,
            Gender = input.Gender,
            ActivityLevel = input.ActivityLevel,
            Goal = input.Goal
        });

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

        dbContext.KitchenSubscriptions.Add(new KitchenSubscription
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
        });

        await dbContext.SaveChangesAsync();
        TempData["SuccessMessage"] = "Kitchen aboneliğin oluşturuldu.";

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
                    RemainingDays = Math.Max(0, subscription.EndsOn.DayNumber - today.DayNumber + 1)
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

    private async Task<IReadOnlyList<KitchenMenuItemCardViewModel>> BuildMenuItemsAsync()
    {
        return await dbContext.KitchenMenuItems
            .AsNoTracking()
            .Where(item => item.IsActive)
            .OrderBy(item => item.DisplayOrder)
            .ThenBy(item => item.Name)
            .Select(item => new KitchenMenuItemCardViewModel
            {
                Id = item.Id,
                Name = item.Name,
                Category = item.Category.ToString(),
                Calories = item.Calories,
                UnitPrice = item.UnitPrice,
                ProteinGrams = item.ProteinGrams,
                CarbohydrateGrams = item.CarbohydrateGrams,
                FatGrams = item.FatGrams,
                Ingredients = item.Ingredients,
                Allergens = item.Allergens,
                Tags = item.Tags
            })
            .ToListAsync();
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
