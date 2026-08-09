using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using NO23.Web.Data.Seed;
using NO23.Web.Domain.Entities;

namespace NO23.Web.Areas.Identity.Pages.Account;

public class LoginModel(
    SignInManager<ApplicationUser> signInManager) : PageModel
{
    [BindProperty]
    public InputModel Input { get; set; } = new();

    [BindProperty]
    public string? ReturnUrl { get; set; }

    [TempData]
    public string? ErrorMessage { get; set; }

    public void OnGet(string? returnUrl = null)
    {
        ReturnUrl = returnUrl;

        if (!string.IsNullOrWhiteSpace(ErrorMessage))
        {
            ModelState.AddModelError(string.Empty, ErrorMessage);
        }
    }

    public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
    {
        ReturnUrl = returnUrl ?? ReturnUrl;

        if (!ModelState.IsValid)
        {
            return Page();
        }

        var result = await signInManager.PasswordSignInAsync(
            Input.Email,
            Input.Password,
            Input.RememberMe,
            lockoutOnFailure: true);

        if (result.Succeeded)
        {
            var signedInUser = await signInManager.UserManager.FindByEmailAsync(Input.Email);

            if (signedInUser is not null &&
                await signInManager.UserManager.IsInRoleAsync(signedInUser, ApplicationRoles.Admin))
            {
                return LocalRedirect("~/Admin/Dashboard");
            }

            if (signedInUser is not null &&
                await signInManager.UserManager.IsInRoleAsync(
                    signedInUser,
                    ApplicationRoles.Trainer))
            {
                if (!string.IsNullOrWhiteSpace(ReturnUrl) &&
                    Url.IsLocalUrl(ReturnUrl))
                {
                    return LocalRedirect(ReturnUrl);
                }

                return LocalRedirect("~/Trainer/Dashboard");
            }

            if (signedInUser is not null &&
                await signInManager.UserManager.IsInRoleAsync(signedInUser, ApplicationRoles.Member))
            {
                if (!string.IsNullOrWhiteSpace(ReturnUrl) &&
                    Url.IsLocalUrl(ReturnUrl))
                {
                    return LocalRedirect(ReturnUrl);
                }

                return LocalRedirect("~/Member/Home");
            }

            return LocalRedirect(ReturnUrl ?? Url.Content("~/"));
        }

        if (result.RequiresTwoFactor)
        {
            return RedirectToPage(
                "./LoginWith2fa",
                new
                {
                    ReturnUrl,
                    Input.RememberMe
                });
        }

        if (result.IsLockedOut)
        {
            ModelState.AddModelError(
                string.Empty,
                "Çok fazla başarısız giriş denemesi yapıldı. Lütfen daha sonra tekrar dene.");

            return Page();
        }

        ModelState.AddModelError(
            string.Empty,
            "E-posta veya parola hatalı.");

        return Page();
    }

    public class InputModel
    {
        [Required(ErrorMessage = "E-posta alanı zorunludur.")]
        [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi girmelisin.")]
        [Display(Name = "E-posta")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Parola alanı zorunludur.")]
        [DataType(DataType.Password)]
        [Display(Name = "Parola")]
        public string Password { get; set; } = string.Empty;

        [Display(Name = "Beni hatırla")]
        public bool RememberMe { get; set; }
    }
}
