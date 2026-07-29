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
public class GoalsController(
    ApplicationDbContext dbContext,
    CommunityChallengeProgressService challengeProgressService) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var model = await BuildViewModelAsync();

        return model is null ? Challenge() : View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(MemberGoalsIndexViewModel model)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Challenge();
        }

        var profile = await dbContext.MemberProfiles
            .Include(member => member.MembershipPackage)
            .FirstOrDefaultAsync(member => member.ApplicationUserId == userId);

        if (profile is null)
        {
            return Challenge();
        }

        if (!ModelState.IsValid)
        {
            PopulateMembershipFields(model, profile);
            model.ChallengeProgressCards = await BuildChallengeProgressCardsAsync(profile.Id);
            return View(model);
        }

        profile.FitnessGoal = string.IsNullOrWhiteSpace(model.FitnessGoal)
            ? null
            : model.FitnessGoal.Trim();
        profile.UpdatedAtUtc = DateTime.UtcNow;

        await dbContext.SaveChangesAsync();

        TempData["SuccessMessage"] = "Antrenman hedefin güncellendi.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> LogChallengeCalories(ChallengeCalorieLogInputViewModel input)
    {
        if (!ModelState.IsValid)
        {
            TempData["ErrorMessage"] = ModelState
                .Where(item => item.Value?.Errors.Count > 0)
                .SelectMany(item => item.Value!.Errors)
                .Select(error => error.ErrorMessage)
                .FirstOrDefault(error => !string.IsNullOrWhiteSpace(error)) ??
                "Kalori girişini kontrol et.";

            return RedirectToAction(nameof(Index));
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Challenge();
        }

        var result = await challengeProgressService.UpsertDailyCaloriesAsync(
            userId,
            new ChallengeCalorieLogRequest(
                input.ParticipationId,
                input.EntryDate,
                input.CaloriesConsumed));

        TempData[result.Succeeded ? "SuccessMessage" : "ErrorMessage"] = result.Message;
        return RedirectToAction(nameof(Index));
    }

    private async Task<MemberGoalsIndexViewModel?> BuildViewModelAsync()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(userId))
        {
            return null;
        }

        var profile = await dbContext.MemberProfiles
            .AsNoTracking()
            .Include(member => member.MembershipPackage)
            .FirstOrDefaultAsync(member => member.ApplicationUserId == userId);

        if (profile is null)
        {
            return null;
        }

        var model = new MemberGoalsIndexViewModel
        {
            FitnessGoal = profile.FitnessGoal,
            ChallengeProgressCards = await BuildChallengeProgressCardsAsync(profile.Id)
        };

        PopulateMembershipFields(model, profile);
        return model;
    }

    private async Task<IReadOnlyList<MemberChallengeProgressCardViewModel>> BuildChallengeProgressCardsAsync(
        int memberProfileId)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var participations = await dbContext.CommunityChallengeParticipations
            .AsNoTracking()
            .Include(item => item.CommunityChallenge)
            .Include(item => item.ProgressEntries)
            .Where(item =>
                item.MemberProfileId == memberProfileId &&
                item.Status != CommunityChallengeParticipationStatus.Withdrawn &&
                (item.CommunityChallenge.Status == CommunityChallengeStatus.Upcoming ||
                 item.CommunityChallenge.Status == CommunityChallengeStatus.Active))
            .OrderBy(item => item.CommunityChallenge.StartsOn)
            .ThenBy(item => item.CommunityChallenge.Title)
            .ToListAsync();

        return participations
            .Select(participation =>
            {
                var challenge = participation.CommunityChallenge;
                var range = CommunityChallengeProgressCalculator.GetCalorieRange(
                    challenge.TargetDailyCalories,
                    challenge.CalorieTolerancePercent);
                var stats = CommunityChallengeProgressCalculator.GetProgressStats(
                    challenge.StartsOn,
                    challenge.EndsOn,
                    challenge.RequiredCompletionPercent,
                    participation.ProgressEntries);
                var todayEntry = participation.ProgressEntries
                    .FirstOrDefault(entry => entry.EntryDate == today);

                return new MemberChallengeProgressCardViewModel
                {
                    ParticipationId = participation.Id,
                    Title = challenge.Title,
                    Status = challenge.Status.ToString(),
                    StartsOn = challenge.StartsOn,
                    EndsOn = challenge.EndsOn,
                    TargetDailyCalories = challenge.TargetDailyCalories,
                    MinDailyCalories = range.MinCalories,
                    MaxDailyCalories = range.MaxCalories,
                    RequiredCompletionPercent = challenge.RequiredCompletionPercent,
                    LoggedDays = stats.LoggedDays,
                    CompliantDays = stats.CompliantDays,
                    TotalDays = stats.TotalDays,
                    ProgressPercent = stats.ProgressPercent,
                    IsCompleted = participation.Status == CommunityChallengeParticipationStatus.Completed,
                    CanLogToday =
                        challenge.Status == CommunityChallengeStatus.Active &&
                        today >= challenge.StartsOn &&
                        today <= challenge.EndsOn,
                    LogDate = today,
                    TodayCaloriesConsumed = todayEntry?.CaloriesConsumed,
                    TodayIsCompliant = todayEntry?.IsCompliant
                };
            })
            .ToList();
    }

    private static void PopulateMembershipFields(
        MemberGoalsIndexViewModel model,
        MemberProfile profile)
    {
        var package = profile.MembershipPackage;

        model.MembershipPackageName = package.Name;
        model.MembershipPackageAudience = package.Audience;
        model.MembershipPackageDescription = package.Description;
        model.RemainingClassCredits = profile.RemainingClassCredits;
        model.HasUnlimitedClasses = package.WeeklyClassLimit is null;
        model.IncludedBenefits = BuildIncludedBenefits(package);
    }

    private static IReadOnlyList<string> BuildIncludedBenefits(
        MembershipPackage package)
    {
        var benefits = new List<string>();

        AddIf(package.IncludesMeasurement, "Ölçüm takibi");
        AddIf(package.IncludesBodyAnalysis, "Vücut analizi");
        AddIf(package.IncludesNutritionSupport, "Beslenme desteği");
        AddIf(package.IncludesDetailedTracking, "Detaylı gelişim takibi");
        AddIf(package.IncludesMonthlyAnalysis, "Aylık performans analizi");
        AddIf(package.IncludesPriorityReservation, "Öncelikli rezervasyon");
        AddIf(package.IncludesPersonalTrainingSupport, "Birebir antrenman desteği");
        AddIf(package.IncludesKitchenBenefits, "NO23 Kitchen avantajları");
        AddIf(package.IncludesPrivateEvents, "Özel etkinlik erişimi");
        AddIf(package.IncludesCommunityMembership, "Community üyeliği");

        return benefits;

        void AddIf(bool condition, string benefit)
        {
            if (condition)
            {
                benefits.Add(benefit);
            }
        }
    }
}
