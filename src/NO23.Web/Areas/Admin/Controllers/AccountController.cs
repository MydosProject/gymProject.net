using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using NO23.Web.Data.Seed;
using NO23.Web.Domain.Entities;
using NO23.Web.ViewModels.Admin;

namespace NO23.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = ApplicationRoles.Admin)]
public class AccountController(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager) : Controller
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
    public async Task<IActionResult> UpdateProfile(
        [Bind(Prefix = "Profile")] AdminProfileInputViewModel input)
    {
        var user = await userManager.GetUserAsync(User);

        if (user is null)
        {
            return Challenge();
        }

        if (!ModelState.IsValid)
        {
            return View("Index", await BuildViewModelAsync(user, profile: input));
        }

        var currentPhoneNumber = await userManager.GetPhoneNumberAsync(user);
        var requestedPhoneNumber = string.IsNullOrWhiteSpace(input.PhoneNumber)
            ? null
            : input.PhoneNumber.Trim();

        user.FirstName = input.FirstName.Trim();
        user.LastName = input.LastName.Trim();

        var result = await userManager.UpdateAsync(user);

        if (!result.Succeeded)
        {
            AddIdentityErrors(result);
            return View("Index", await BuildViewModelAsync(user, profile: input));
        }

        if (!string.Equals(
                currentPhoneNumber,
                requestedPhoneNumber,
                StringComparison.Ordinal))
        {
            var phoneResult = await userManager.SetPhoneNumberAsync(
                user,
                requestedPhoneNumber);

            if (!phoneResult.Succeeded)
            {
                AddIdentityErrors(phoneResult);
                return View("Index", await BuildViewModelAsync(user, profile: input));
            }
        }

        await signInManager.RefreshSignInAsync(user);
        TempData["SuccessMessage"] = "Yönetici profil bilgilerin güncellendi.";

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(
        [Bind(Prefix = "ChangePassword")] AdminChangePasswordInputViewModel input)
    {
        var user = await userManager.GetUserAsync(User);

        if (user is null)
        {
            return Challenge();
        }

        if (!ModelState.IsValid)
        {
            return View("Index", await BuildViewModelAsync(user, password: input));
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

            return View("Index", await BuildViewModelAsync(user, password: input));
        }

        await signInManager.RefreshSignInAsync(user);
        TempData["SuccessMessage"] = "Şifren başarıyla güncellendi.";

        return RedirectToAction(nameof(Index));
    }

    private async Task<AdminAccountSettingsViewModel> BuildViewModelAsync(
        ApplicationUser user,
        AdminProfileInputViewModel? profile = null,
        AdminChangePasswordInputViewModel? password = null)
    {
        return new AdminAccountSettingsViewModel
        {
            Email = await userManager.GetEmailAsync(user) ?? string.Empty,
            EmailConfirmed = await userManager.IsEmailConfirmedAsync(user),
            TwoFactorEnabled = await userManager.GetTwoFactorEnabledAsync(user),
            HasPassword = await userManager.HasPasswordAsync(user),
            CreatedAtUtc = user.CreatedAtUtc,
            LastLoginAtUtc = user.LastLoginAtUtc,
            Profile = profile ?? new AdminProfileInputViewModel
            {
                FirstName = user.FirstName ?? string.Empty,
                LastName = user.LastName ?? string.Empty,
                PhoneNumber = await userManager.GetPhoneNumberAsync(user)
            },
            ChangePassword = password ?? new AdminChangePasswordInputViewModel()
        };
    }

    private void AddIdentityErrors(IdentityResult result)
    {
        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }
    }
}
