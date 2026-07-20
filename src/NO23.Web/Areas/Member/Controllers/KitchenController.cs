using System.Security.Claims;
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
    public async Task<IActionResult> Index()
    {
        return View(await BuildDashboardAsync(new CalorieCalculatorInputViewModel(), null));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Calculate([Bind(Prefix = "CalculatorInput")] CalorieCalculatorInputViewModel input)
    {
        if (!ModelState.IsValid)
        {
            return View("Index", await BuildDashboardAsync(input, null));
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

        return View("Index", await BuildDashboardAsync(input, new CalorieRecommendationViewModel
        {
            Goal = input.Goal,
            DailyCalories = result.DailyCalories,
            ProteinGrams = result.ProteinGrams,
            CarbohydrateGrams = result.CarbohydrateGrams,
            FatGrams = result.FatGrams
        }));
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
        var menuItems = await dbContext.KitchenMenuItems
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
                ProteinGrams = item.ProteinGrams,
                CarbohydrateGrams = item.CarbohydrateGrams,
                FatGrams = item.FatGrams,
                Ingredients = item.Ingredients,
                Allergens = item.Allergens,
                Tags = item.Tags
            })
            .ToListAsync();

        return new KitchenDashboardViewModel
        {
            CalculatorInput = input,
            Recommendation = recommendation,
            MenuItems = menuItems,
            SubscriptionPlans =
            [
                new() { Plan = KitchenSubscriptionPlan.FiveDays, Name = "5 Günlük", Days = 5 },
                new() { Plan = KitchenSubscriptionPlan.TenDays, Name = "10 Günlük", Days = 10 },
                new() { Plan = KitchenSubscriptionPlan.TwentyDays, Name = "20 Günlük", Days = 20 },
                new() { Plan = KitchenSubscriptionPlan.Monthly, Name = "Aylık", Days = 30 }
            ]
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
}
