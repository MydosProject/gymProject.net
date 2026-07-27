using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using NO23.Web.Data.Seed;
using NO23.Web.Domain.Entities;
using NO23.Web.ViewModels.Member;

namespace NO23.Web.Areas.Member.Controllers;

[Area("Member")]
[Authorize(Roles = ApplicationRoles.Member)]
public class SettingsController(
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

                ModelState.AddModelError(field, TranslateIdentityError(error));
            }

            return View("Index", await BuildViewModelAsync(user, input));
        }

        await signInManager.RefreshSignInAsync(user);

        TempData["SuccessMessage"] = "Şifren başarıyla güncellendi.";
        return RedirectToAction(nameof(Index));
    }

    private async Task<MemberAccountSettingsViewModel> BuildViewModelAsync(
        ApplicationUser user,
        ChangePasswordInputViewModel? input = null)
    {
        return new MemberAccountSettingsViewModel
        {
            Email = await userManager.GetEmailAsync(user) ?? string.Empty,
            PhoneNumber = await userManager.GetPhoneNumberAsync(user),
            EmailConfirmed = await userManager.IsEmailConfirmedAsync(user),
            TwoFactorEnabled = await userManager.GetTwoFactorEnabledAsync(user),
            HasPassword = await userManager.HasPasswordAsync(user),
            ChangePassword = input ?? new ChangePasswordInputViewModel()
        };
    }

    private static string TranslateIdentityError(IdentityError error)
    {
        return error.Code switch
        {
            "PasswordMismatch" => "Mevcut şifren doğru değil.",
            "PasswordTooShort" => "Yeni şifren yeterince uzun değil.",
            "PasswordRequiresNonAlphanumeric" =>
                "Yeni şifrende en az bir özel karakter bulunmalıdır.",
            "PasswordRequiresDigit" =>
                "Yeni şifrende en az bir rakam bulunmalıdır.",
            "PasswordRequiresLower" =>
                "Yeni şifrende en az bir küçük harf bulunmalıdır.",
            "PasswordRequiresUpper" =>
                "Yeni şifrende en az bir büyük harf bulunmalıdır.",
            _ => error.Description
        };
    }
}
