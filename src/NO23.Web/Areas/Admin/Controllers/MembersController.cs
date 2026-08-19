using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NO23.Web.Data;
using NO23.Web.Data.Seed;
using NO23.Web.ViewModels.Admin;

namespace NO23.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = ApplicationRoles.Admin)]
public class MembersController(ApplicationDbContext dbContext) : Controller
{
    public async Task<IActionResult> Index()
    {
        var members = await dbContext.MemberProfiles
            .AsNoTracking()
            .Include(profile => profile.ApplicationUser)
            .Include(profile => profile.MembershipPackage)
            .OrderByDescending(profile => profile.CreatedAtUtc)
            .Select(profile => new MemberListItemViewModel
            {
                Id = profile.ApplicationUserId,
                FullName = ((profile.ApplicationUser.FirstName ?? "") + " " + (profile.ApplicationUser.LastName ?? "")).Trim(),
                Email = profile.ApplicationUser.Email ?? "",
                PhoneNumber = profile.ApplicationUser.PhoneNumber,
                PackageName = profile.MembershipPackage.Name,
                FitnessGoal = profile.FitnessGoal,
                RemainingClassCredits = profile.RemainingClassCredits,
                IsSuspended = profile.IsSuspended,
                SuspendedAtUtc = profile.SuspendedAtUtc,
                CreatedAtUtc = profile.CreatedAtUtc
            })
            .ToListAsync();

        return View(members);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Suspend(string id)
    {
        var profile = await dbContext.MemberProfiles
            .FirstOrDefaultAsync(item => item.ApplicationUserId == id);

        if (profile is null)
        {
            return NotFound();
        }

        if (!profile.IsSuspended)
        {
            profile.IsSuspended = true;
            profile.SuspendedAtUtc = DateTime.UtcNow;
            profile.UpdatedAtUtc = DateTime.UtcNow;

            await dbContext.SaveChangesAsync();
        }

        TempData["SuccessMessage"] = "Üyelik askıya alındı.";

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reactivate(string id)
    {
        var profile = await dbContext.MemberProfiles
            .FirstOrDefaultAsync(item => item.ApplicationUserId == id);

        if (profile is null)
        {
            return NotFound();
        }

        if (profile.IsSuspended)
        {
            profile.IsSuspended = false;
            profile.SuspendedAtUtc = null;
            profile.SuspensionReason = null;
            profile.UpdatedAtUtc = DateTime.UtcNow;

            await dbContext.SaveChangesAsync();
        }

        TempData["SuccessMessage"] = "Üyelik yeniden aktifleştirildi.";

        return RedirectToAction(nameof(Index));
    }
}