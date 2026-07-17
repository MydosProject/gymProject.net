using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using NO23.Web.Data;
using NO23.Web.Data.Seed;
using NO23.Web.Domain.Entities;
using NO23.Web.Domain.Enums;

namespace NO23.Web.Areas.Identity.Pages.Account;

public class RegisterModel(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager,
    ApplicationDbContext dbContext) : PageModel
{
    [BindProperty]
    public InputModel Input { get; set; } = new();

    [BindProperty]
    public string? ReturnUrl { get; set; }

    public IReadOnlyList<PackageOption> PackageOptions { get; private set; } = [];

    public async Task OnGetAsync(string? package = null, string? returnUrl = null)
    {
        ReturnUrl = returnUrl;
        await LoadPackageOptionsAsync(package);
    }

    public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
    {
        ReturnUrl = returnUrl ?? ReturnUrl;
        await LoadPackageOptionsAsync(Input.PackageCode);

        var selectedPackage = await FindSelectedPackageAsync(Input.PackageCode);
        if (selectedPackage is null)
        {
            ModelState.AddModelError(nameof(Input.PackageCode), "Geçerli bir üyelik paketi seçmelisin.");
        }

        if (!ModelState.IsValid)
        {
            return Page();
        }

        var user = new ApplicationUser
        {
            UserName = Input.Email,
            Email = Input.Email,
            EmailConfirmed = true,
            PhoneNumber = Input.PhoneNumber,
            FirstName = Input.FirstName,
            LastName = Input.LastName
        };

        var createResult = await userManager.CreateAsync(user, Input.Password);

        if (!createResult.Succeeded)
        {
            foreach (var error in createResult.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return Page();
        }

        await userManager.AddToRoleAsync(user, ApplicationRoles.Member);

        dbContext.MemberProfiles.Add(new MemberProfile
        {
            ApplicationUserId = user.Id,
            MembershipPackageId = selectedPackage!.Id,
            FitnessGoal = Input.FitnessGoal,
            RemainingClassCredits = CalculateInitialClassCredits(selectedPackage)
        });

        await dbContext.SaveChangesAsync();
        await signInManager.SignInAsync(user, isPersistent: false);

        return LocalRedirect(ReturnUrl ?? Url.Content("~/"));
    }

    private async Task LoadPackageOptionsAsync(string? selectedPackageCode)
    {
        PackageOptions = await dbContext.MembershipPackages
            .AsNoTracking()
            .Where(package => package.IsActive)
            .OrderBy(package => package.DisplayOrder)
            .Select(package => new PackageOption(
                package.Code.ToString().ToUpper(),
                package.Name,
                package.Audience))
            .ToListAsync();

        Input.PackageCode = ResolvePackageCode(selectedPackageCode)
            ?? PackageOptions.FirstOrDefault()?.Code
            ?? string.Empty;
    }

    private async Task<MembershipPackage?> FindSelectedPackageAsync(string? packageCode)
    {
        if (!Enum.TryParse<MembershipPackageCode>(packageCode, ignoreCase: true, out var parsedCode))
        {
            return null;
        }

        return await dbContext.MembershipPackages
            .FirstOrDefaultAsync(package => package.Code == parsedCode && package.IsActive);
    }

    private static string? ResolvePackageCode(string? packageCode)
    {
        return Enum.TryParse<MembershipPackageCode>(packageCode, ignoreCase: true, out var parsedCode)
            ? parsedCode.ToString().ToUpper()
            : null;
    }

    private static int CalculateInitialClassCredits(MembershipPackage package)
    {
        return package.WeeklyClassLimit.HasValue
            ? package.WeeklyClassLimit.Value * 4
            : 0;
    }

    public class InputModel
    {
        [Required(ErrorMessage = "Ad alanı zorunludur.")]
        [StringLength(80)]
        [Display(Name = "Ad")]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Soyad alanı zorunludur.")]
        [StringLength(80)]
        [Display(Name = "Soyad")]
        public string LastName { get; set; } = string.Empty;

        [Required(ErrorMessage = "E-posta alanı zorunludur.")]
        [EmailAddress]
        [Display(Name = "E-posta")]
        public string Email { get; set; } = string.Empty;

        [Phone]
        [Display(Name = "Telefon")]
        public string? PhoneNumber { get; set; }

        [Required(ErrorMessage = "Üyelik paketi seçmelisin.")]
        [Display(Name = "Üyelik paketi")]
        public string PackageCode { get; set; } = string.Empty;

        [StringLength(160)]
        [Display(Name = "Hedef")]
        public string? FitnessGoal { get; set; }

        [Required(ErrorMessage = "Parola alanı zorunludur.")]
        [StringLength(100, ErrorMessage = "{0} en az {2}, en fazla {1} karakter olmalıdır.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Parola")]
        public string Password { get; set; } = string.Empty;

        [DataType(DataType.Password)]
        [Display(Name = "Parola tekrar")]
        [Compare(nameof(Password), ErrorMessage = "Parola ve tekrar alanı eşleşmiyor.")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    public record PackageOption(string Code, string Name, string Audience);
}
