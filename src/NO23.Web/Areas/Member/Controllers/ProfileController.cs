using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NO23.Web.Data;
using NO23.Web.Data.Seed;
using NO23.Web.Domain.Entities;
using NO23.Web.ViewModels.Member;

namespace NO23.Web.Areas.Member.Controllers;

[Area("Member")]
[Authorize(Roles = ApplicationRoles.Member)]
public class ProfileController(
    ApplicationDbContext dbContext,
    UserManager<ApplicationUser> userManager) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var model = await BuildViewModelAsync();

        return model is null ? Challenge() : View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(MemberProfileIndexViewModel model)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Challenge();
        }

        var user = await userManager.FindByIdAsync(userId);

        if (user is null)
        {
            return Challenge();
        }

        if (!ModelState.IsValid)
        {
            await PopulateReadOnlyFieldsAsync(model, userId, user);
            return View(model);
        }

        user.FirstName = model.FirstName.Trim();
        user.LastName = model.LastName.Trim();
        user.PhoneNumber = string.IsNullOrWhiteSpace(model.PhoneNumber)
            ? null
            : model.PhoneNumber.Trim();

        var result = await userManager.UpdateAsync(user);

        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            await PopulateReadOnlyFieldsAsync(model, userId, user);
            return View(model);
        }

        var profile = await dbContext.MemberProfiles
            .FirstOrDefaultAsync(member => member.ApplicationUserId == userId);

        if (profile is not null)
        {
            profile.UpdatedAtUtc = DateTime.UtcNow;
            await dbContext.SaveChangesAsync();
        }

        TempData["SuccessMessage"] = "Profil bilgilerin güncellendi.";
        return RedirectToAction(nameof(Index));
    }

    private async Task<MemberProfileIndexViewModel?> BuildViewModelAsync()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(userId))
        {
            return null;
        }

        var user = await userManager.FindByIdAsync(userId);

        if (user is null)
        {
            return null;
        }

        var model = new MemberProfileIndexViewModel
        {
            FirstName = user.FirstName ?? string.Empty,
            LastName = user.LastName ?? string.Empty,
            PhoneNumber = user.PhoneNumber
        };

        await PopulateReadOnlyFieldsAsync(model, userId, user);
        return model;
    }

    private async Task PopulateReadOnlyFieldsAsync(
        MemberProfileIndexViewModel model,
        string userId,
        ApplicationUser user)
    {
        var membership = await dbContext.MemberProfiles
            .AsNoTracking()
            .Where(member => member.ApplicationUserId == userId)
            .Select(member => new
            {
                PackageName = member.MembershipPackage.Name,
                member.CreatedAtUtc
            })
            .FirstOrDefaultAsync();

        model.Email = user.Email ?? string.Empty;
        model.MembershipPackageName = membership?.PackageName ?? "Üyelik bilgisi bulunamadı";
        model.MemberSinceUtc = membership?.CreatedAtUtc ?? user.CreatedAtUtc;
    }
}
