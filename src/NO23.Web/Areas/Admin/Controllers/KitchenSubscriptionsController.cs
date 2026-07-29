using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NO23.Web.Data;
using NO23.Web.Data.Seed;
using NO23.Web.ViewModels.Admin;

namespace NO23.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = ApplicationRoles.Admin)]
public class KitchenSubscriptionsController(ApplicationDbContext dbContext) : Controller
{
    public async Task<IActionResult> Index()
    {
        var subscriptions = await dbContext.KitchenSubscriptions
            .AsNoTracking()
            .Include(subscription => subscription.MemberProfile)
            .ThenInclude(profile => profile.ApplicationUser)
            .OrderByDescending(subscription => subscription.CreatedAtUtc)
            .Select(subscription => new KitchenSubscriptionListItemViewModel
            {
                Id = subscription.Id,
                MemberName = ((subscription.MemberProfile.ApplicationUser.FirstName ?? "") + " " +
                    (subscription.MemberProfile.ApplicationUser.LastName ?? "")).Trim(),
                MemberEmail = subscription.MemberProfile.ApplicationUser.Email ?? "",
                Plan = subscription.Plan.ToString(),
                Goal = subscription.Goal.ToString(),
                Status = subscription.Status.ToString(),
                PackageName = subscription.PackageNameSnapshot,
                PackagePrice = subscription.PackagePriceSnapshot,
                PackageDays = subscription.PackageDaysSnapshot,
                DailyCalories = subscription.DailyCalories,
                ProteinGrams = subscription.ProteinGrams,
                CarbohydrateGrams = subscription.CarbohydrateGrams,
                FatGrams = subscription.FatGrams,
                StartsOn = subscription.StartsOn,
                EndsOn = subscription.EndsOn
            })
            .ToListAsync();

        return View(subscriptions);
    }
}
