using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using NO23.Web.Data;
using NO23.Web.Data.Seed;
using NO23.Web.Domain.Entities;
using NO23.Web.Infrastructure.Identity;

namespace NO23.Web.Areas.Identity.Pages.Account;

public class LoginModel(
    SignInManager<ApplicationUser> signInManager,
    ApplicationDbContext dbContext) : PageModel
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

        var signedInUser = await FindUserAsync(Input.Identifier);
        var result = signedInUser is null
            ? Microsoft.AspNetCore.Identity.SignInResult.Failed
            : await signInManager.PasswordSignInAsync(
                signedInUser,
                Input.Password,
                Input.RememberMe,
                lockoutOnFailure: true);

        if (result.Succeeded)
        {
            if (await signInManager.UserManager.IsInRoleAsync(
                    signedInUser!,
                    ApplicationRoles.Admin))
            {
                return LocalRedirect("~/Admin/Dashboard");
            }

            if (await signInManager.UserManager.IsInRoleAsync(
                    signedInUser!,
                    ApplicationRoles.Trainer))
            {
                if (!string.IsNullOrWhiteSpace(ReturnUrl) &&
                    Url.IsLocalUrl(ReturnUrl))
                {
                    return LocalRedirect(ReturnUrl);
                }

                return LocalRedirect("~/Trainer/Dashboard");
            }

            if (await signInManager.UserManager.IsInRoleAsync(
                    signedInUser!,
                    ApplicationRoles.Member))
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
            "Ad soyad/e-posta veya parola hatalı.");

        return Page();
    }

    private async Task<ApplicationUser?> FindUserAsync(string identifier)
    {
        var cleanedIdentifier = MemberLoginName.CleanPart(identifier);

        if (cleanedIdentifier.Contains('@'))
        {
            return await signInManager.UserManager.FindByEmailAsync(
                cleanedIdentifier);
        }

        var currentMember = await signInManager.UserManager.FindByNameAsync(
            MemberLoginName.BuildUserName(cleanedIdentifier));

        if (currentMember is not null)
        {
            return currentMember;
        }

        var legacyMembers = await dbContext.MemberProfiles
            .AsNoTracking()
            .Select(profile => new
            {
                profile.ApplicationUserId,
                profile.ApplicationUser.FirstName,
                profile.ApplicationUser.LastName
            })
            .ToListAsync();

        var matchingMemberIds = legacyMembers
            .Where(member => MemberLoginName.Matches(
                cleanedIdentifier,
                member.FirstName,
                member.LastName))
            .Select(member => member.ApplicationUserId)
            .Take(2)
            .ToList();

        return matchingMemberIds.Count == 1
            ? await signInManager.UserManager.FindByIdAsync(
                matchingMemberIds[0])
            : null;
    }

    public class InputModel
    {
        [Required(ErrorMessage = "Ad soyad alanı zorunludur.")]
        [StringLength(161)]
        [Display(Name = "Ad Soyad")]
        public string Identifier { get; set; } = string.Empty;

        [Required(ErrorMessage = "Parola alanı zorunludur.")]
        [DataType(DataType.Password)]
        [Display(Name = "Parola")]
        public string Password { get; set; } = string.Empty;

        [Display(Name = "Beni hatırla")]
        public bool RememberMe { get; set; }
    }
}
