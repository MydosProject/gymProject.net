using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NO23.Web.Data;
using NO23.Web.Data.Seed;
using NO23.Web.Domain.Entities;
using NO23.Web.ViewModels.Member;

namespace NO23.Web.Areas.Member.Controllers;

[Area("Member")]
[Authorize(Roles = ApplicationRoles.Member)]
public class GoalsController(ApplicationDbContext dbContext) : Controller
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
            FitnessGoal = profile.FitnessGoal
        };

        PopulateMembershipFields(model, profile);
        return model;
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
