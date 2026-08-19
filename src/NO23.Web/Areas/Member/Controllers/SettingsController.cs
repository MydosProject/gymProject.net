using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NO23.Web.Data;
using NO23.Web.Data.Seed;
using NO23.Web.Domain.Entities;
using NO23.Web.Domain.Enums;
using NO23.Web.ViewModels.Member;
using NO23.Web.Services;

namespace NO23.Web.Areas.Member.Controllers;

[Area("Member")]
[Authorize(Roles = ApplicationRoles.Member)]
public class SettingsController(
    ApplicationDbContext dbContext,
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager,
    MemberMembershipService membershipService) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var user = await userManager.GetUserAsync(User);

        return user is null
            ? Challenge()
            : View(await BuildViewModelAsync(user));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(
        [Bind(Prefix = "ChangePassword")] ChangePasswordInputViewModel input)
    {
        var user = await userManager.GetUserAsync(User);

        if (user is null)
        {
            return Challenge();
        }

        if (!ModelState.IsValid)
        {
            return View("Index", await BuildViewModelAsync(user, input));
        }

        var result = await userManager.ChangePasswordAsync(
            user,
            input.CurrentPassword,
            input.NewPassword);

        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                var field = error.Code == "PasswordMismatch"
                    ? "ChangePassword.CurrentPassword"
                    : string.Empty;

                ModelState.AddModelError(field, error.Description);
            }

            return View("Index", await BuildViewModelAsync(user, input));
        }

        await signInManager.RefreshSignInAsync(user);

        TempData["SuccessMessage"] = "Şifren başarıyla güncellendi.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CancelMembership()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Challenge();
        }

        var result =
            await membershipService.ScheduleCancellationAsync(userId);

        if (result.Succeeded)
        {
            TempData["SuccessMessage"] =
                result.CancelledReservationCount > 0
                    ? "Üyelik paketin dönem sonunda iptal edilecek. Paket bitişinden sonraki rezervasyonların iptal edildi."
                    : "Üyelik paketin dönem sonunda iptal edilecek.";
        }
        else
        {
            TempData["ErrorMessage"] = result.ErrorMessage;
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RequestMembershipPackageChange(
        int packageId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Challenge();
        }

        var result = await membershipService.RequestPackageChangeAsync(
            userId,
            packageId);

        TempData[result.Succeeded ? "SuccessMessage" : "ErrorMessage"] =
            result.Succeeded
                ? "Paket talebin alındı. Ekip onayladığında üyelik dönemine yansıtılacak."
                : result.ErrorMessage;

        return RedirectToAction(nameof(Index));
    }

    private async Task<MemberAccountSettingsViewModel> BuildViewModelAsync(
        ApplicationUser user,
        ChangePasswordInputViewModel? input = null)
    {
        var nowUtc = DateTime.UtcNow;
        var membership = await dbContext.MemberProfiles
            .AsNoTracking()
            .Where(member => member.ApplicationUserId == user.Id)
            .Select(member => new
            {
                member.Id,
                CurrentPackageId = member.MembershipPackageId,
                PackageName = member.MembershipPackage.Name,
                member.MembershipStartsAtUtc,
                member.MembershipEndsAtUtc,
                member.MembershipStatus,
                member.MembershipCancellationRequestedAtUtc,
                member.MembershipCancellationEffectiveAtUtc
            })
            .FirstOrDefaultAsync();

        var pendingPackageChangeRequest = membership is null
            ? null
            : await dbContext.MembershipPackageChangeRequests
                .AsNoTracking()
                .Where(request =>
                    request.MemberProfileId == membership.Id &&
                    request.Status ==
                        MembershipPackageChangeRequestStatus.Pending)
                .Select(request => new
                {
                    RequestedPackageName =
                        request.RequestedMembershipPackage.Name
                })
                .FirstOrDefaultAsync();

        var packageOptions = await dbContext.MembershipPackages
            .AsNoTracking()
            .Where(package => package.IsActive)
            .OrderBy(package => package.DisplayOrder)
            .Select(package => new MemberMembershipPackageOptionViewModel
            {
                Id = package.Id,
                Name = package.Name,
                Audience = package.Audience,
                Description = package.Description,
                ClassAccessSummary = package.WeeklyClassLimit.HasValue
                    ? $"4 haftada {package.WeeklyClassLimit.Value * 4} ders hakkı"
                    : "Sınırsız grup dersi",
                IsCurrentPackage =
                    membership != null &&
                    package.Id == membership.CurrentPackageId
            })
            .ToListAsync();

        var canCancelMembership = membership is not null &&
            membership.MembershipStatus == MembershipStatus.Active &&
            MemberMembershipService.IsActiveForUse(
                membership.MembershipStatus,
                membership.MembershipEndsAtUtc,
                nowUtc);

        return new MemberAccountSettingsViewModel
        {
            Email = await userManager.GetEmailAsync(user) ?? string.Empty,
            PhoneNumber = await userManager.GetPhoneNumberAsync(user),
            EmailConfirmed = await userManager.IsEmailConfirmedAsync(user),
            TwoFactorEnabled = await userManager.GetTwoFactorEnabledAsync(user),
            HasPassword = await userManager.HasPasswordAsync(user),
            HasMembershipPackage = membership is not null,
            MembershipPackageName =
                membership?.PackageName ?? "Üyelik paketi bulunamadı",
            MembershipStartsAtUtc = membership?.MembershipStartsAtUtc,
            MembershipEndsAtUtc = membership?.MembershipEndsAtUtc,
            MembershipStatusDisplayName = membership is null
                ? "Üyelik bilgisi bulunamadı"
                : MemberMembershipService.GetEffectiveStatusDisplayName(
                    membership.MembershipStatus,
                    membership.MembershipEndsAtUtc,
                    nowUtc),
            MembershipCancellationRequestedAtUtc =
                membership?.MembershipCancellationRequestedAtUtc,
            MembershipCancellationEffectiveAtUtc =
                membership?.MembershipCancellationEffectiveAtUtc,
            CanCancelMembership = canCancelMembership,
            HasPendingPackageChangeRequest =
                pendingPackageChangeRequest is not null,
            PendingPackageChangeRequestName =
                pendingPackageChangeRequest?.RequestedPackageName,
            MembershipPackageOptions = packageOptions,
            ChangePassword = input ?? new ChangePasswordInputViewModel()
        };
    }

}
