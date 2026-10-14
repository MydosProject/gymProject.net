using System.ComponentModel.DataAnnotations;
using System.Globalization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using NO23.Web.Data;
using NO23.Web.Data.Seed;
using NO23.Web.Domain.Entities;
using NO23.Web.Domain.Enums;
using NO23.Web.Infrastructure.Identity;
using NO23.Web.Infrastructure.Validation;
using NO23.Web.Services;

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
    public IReadOnlyList<ServiceOption> ServiceOptions { get; private set; } = [];

    public async Task<IActionResult> OnGetAsync(
        string? package = null,
        int? option = null,
        string? returnUrl = null,
        int? variant = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return LocalRedirect(Url.Content("~/plans/membership"));
        }

        ReturnUrl = returnUrl;
        await LoadPackageOptionsAsync(package, variant);

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(
        string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return LocalRedirect(Url.Content("~/plans/membership"));
        }

        ReturnUrl = returnUrl ?? ReturnUrl;
        var postedPackageCode = Input.PackageCode;
        var postedOptionId = Input.ServicePackageVariantId;
        await LoadPackageOptionsAsync(postedPackageCode, postedOptionId);
        Input.PackageCode = postedPackageCode;
        Input.ServicePackageVariantId = postedOptionId;

        var selectedPackage =
            await FindSelectedPackageAsync(Input.PackageCode);

        ServicePackageVariant? selectedVariant = null;
        MemberProfile? referringMember = null;

        if (!string.IsNullOrWhiteSpace(Input.ReferralCode))
        {
            referringMember = await dbContext.MemberProfiles
                .FirstOrDefaultAsync(profile => profile.ReferralCode == Input.ReferralCode.Trim().ToUpper());
            if (referringMember is null)
            {
                ModelState.AddModelError("Input.ReferralCode", "Davet kodu geçerli değil.");
            }
        }

        if (selectedPackage is null)
        {
            ModelState.AddModelError(
                "Input.PackageCode",
                "Geçerli bir üyelik paketi seçmelisin.");
        }
        else
        {
            selectedVariant = await dbContext.ServicePackageVariants
                .FirstOrDefaultAsync(variant =>
                    variant.Id == Input.ServicePackageVariantId &&
                    variant.ServicePackageId == selectedPackage.Id &&
                    variant.IsActive);

            if (selectedVariant is null)
            {
                ModelState.AddModelError(
                    "Input.ServicePackageVariantId",
                    "Bu üyelik paketine ait aktif bir süre seçmelisin.");
            }
        }

        var birthDateError = BirthDateValidation.GetError(Input.BirthDate, required: true);
        if (birthDateError is not null)
        {
            ModelState.AddModelError("Input.BirthDate", birthDateError);
        }

        if (!ModelState.IsValid)
        {
            return Page();
        }

        Input.FirstName = MemberLoginName.CleanPart(Input.FirstName);
        Input.LastName = MemberLoginName.CleanPart(Input.LastName);
        Input.NationalIdentityNumber = Input.NationalIdentityNumber.Trim();
        var memberUserName = MemberLoginName.BuildUserName(
            Input.FirstName,
            Input.LastName);

        if (await dbContext.MemberProfiles.AnyAsync(profile =>
                profile.NationalIdentityNumber == Input.NationalIdentityNumber))
        {
            ModelState.AddModelError(
                "Input.NationalIdentityNumber",
                "Bu TC Kimlik No ile daha önce bir üyelik oluşturulmuş.");

            return Page();
        }

        var existingUser = await userManager.FindByNameAsync(memberUserName);
        var recoverableOrphan = existingUser is not null &&
            !await dbContext.MemberProfiles.AnyAsync(profile =>
                profile.ApplicationUserId == existingUser.Id) &&
            await userManager.IsInRoleAsync(existingUser, ApplicationRoles.Member) &&
            await userManager.CheckPasswordAsync(existingUser, Input.Password) &&
            MemberLoginName.Matches(
                $"{Input.FirstName} {Input.LastName}",
                existingUser.FirstName,
                existingUser.LastName);

        if ((existingUser is not null && !recoverableOrphan) ||
            await HasExistingMemberWithSameNameAsync(
                Input.FirstName,
                Input.LastName))
        {
            ModelState.AddModelError(
                string.Empty,
                "Bu ad soyad ile daha önce bir üyelik oluşturulmuş.");

            return Page();
        }

        await using var transaction = dbContext.Database.IsRelational()
            ? await dbContext.Database.BeginTransactionAsync()
            : null;

        var user = existingUser ?? new ApplicationUser
        {
            UserName = memberUserName,
            PhoneNumber = Input.PhoneNumber,
            FirstName = Input.FirstName,
            LastName = Input.LastName
        };

        if (existingUser is null)
        {
            var createResult = await userManager.CreateAsync(user, Input.Password);
            if (!createResult.Succeeded)
            {
                foreach (var error in createResult.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }

                return Page();
            }
        }
        else
        {
            user.PhoneNumber = Input.PhoneNumber;
            user.FirstName = Input.FirstName;
            user.LastName = Input.LastName;
            var updateResult = await userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                foreach (var error in updateResult.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }

                return Page();
            }
        }

        if (!await userManager.IsInRoleAsync(user, ApplicationRoles.Member))
        {
            var roleResult = await userManager.AddToRoleAsync(user, ApplicationRoles.Member);
            if (!roleResult.Succeeded)
            {
                foreach (var error in roleResult.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }

                return Page();
            }
        }

        var membershipStartsAtUtc = DateTime.UtcNow;
        dbContext.MemberProfiles.Add(new MemberProfile
        {
            ApplicationUserId = user.Id,
            NationalIdentityNumber = Input.NationalIdentityNumber,
            BirthDate = Input.BirthDate,
            MembershipPackageId = selectedPackage!.MembershipPackageId!.Value,
            ServicePackageVariantId = selectedVariant!.Id,
            MembershipStartsAtUtc = membershipStartsAtUtc,
            MembershipEndsAtUtc = MemberPackageEntitlement.CalculateEndDate(
                selectedVariant, membershipStartsAtUtc),
            FitnessGoal = Input.FitnessGoal,
            RemainingClassCredits =
                MemberPackageEntitlement.CalculateInitialCredits(selectedVariant),
            ReferralCode = await GenerateReferralCodeAsync(),
            ReferredByMemberProfileId = referringMember?.Id
        });

        await dbContext.SaveChangesAsync();
        if (transaction is not null) await transaction.CommitAsync();

        await signInManager.SignInAsync(
            user,
            isPersistent: false);

        return LocalRedirect(
            ReturnUrl ?? Url.Content("~/"));
    }

    private async Task LoadPackageOptionsAsync(
        string? selectedPackageCode,
        int? selectedOptionId = null)
    {
        var packages = await dbContext.ServicePackages
            .AsNoTracking()
            .Include(package => package.MembershipPackage)
            .Include(package => package.Variants)
            .Where(package => package.IsActive &&
                package.Category == ServicePackageCategory.Membership &&
                package.MembershipPackageId != null &&
                package.Variants.Any(variant => variant.IsActive))
            .OrderBy(package => package.DisplayOrder)
            .ToListAsync();

        PackageOptions = packages
            .Select(package => new PackageOption(
                package.Slug,
                package.Name,
                package.Subtitle))
            .ToList();

        // Old public links may contain a legacy code. Resolve it through the
        // published catalog; legacy records themselves are never selectable.
        var selectedPackage = packages.FirstOrDefault(package =>
            package.Slug.Equals(selectedPackageCode, StringComparison.OrdinalIgnoreCase))
            ?? packages.FirstOrDefault(package =>
                package.MembershipPackage!.Code.ToString()
                    .Equals(selectedPackageCode, StringComparison.OrdinalIgnoreCase));
        Input.PackageCode = selectedPackage?.Slug
            ?? PackageOptions.FirstOrDefault()?.Code
            ?? string.Empty;

        ServiceOptions = packages.SelectMany(package => package.Variants
            .Where(variant => variant.IsActive)
            .OrderBy(variant => variant.DisplayOrder)
            .Select(variant =>
            {
                var presentation = ServicePackageCatalogService.Present(variant, package.Variants);
                var duration = MemberPackageEntitlement.DurationLabel(variant);
                var totalPrice = variant.BillingType == ServicePackageBillingType.MonthlySubscription &&
                    variant.DurationMonths is > 0 && !variant.PriceOnRequest
                    ? $"{((variant.MonthlyPrice ?? 0) * variant.DurationMonths.Value).ToString("N2", CultureInfo.GetCultureInfo("tr-TR"))} ₺"
                    : null;
                return new ServiceOption(variant.Id, package.Slug, variant.Name,
                    presentation.Price, presentation.PriceNote, duration, presentation.Rights, totalPrice);
            })).ToList();
        var validSelectedOption = ServiceOptions.FirstOrDefault(x =>
            x.Id == selectedOptionId && x.PackageCode.Equals(Input.PackageCode, StringComparison.OrdinalIgnoreCase));
        Input.ServicePackageVariantId = validSelectedOption?.Id ?? ServiceOptions
            .FirstOrDefault(x => x.PackageCode.Equals(Input.PackageCode, StringComparison.OrdinalIgnoreCase))?.Id;
    }

    private async Task<ServicePackage?>
        FindSelectedPackageAsync(string? packageCode)
    {
        return await dbContext.ServicePackages
            .FirstOrDefaultAsync(package =>
                package.Slug == packageCode &&
                package.Category == ServicePackageCategory.Membership &&
                package.MembershipPackageId != null && package.IsActive);
    }

    private async Task<bool> HasExistingMemberWithSameNameAsync(
        string firstName,
        string lastName)
    {
        var existingMemberNames = await dbContext.MemberProfiles
            .AsNoTracking()
            .Select(profile => new
            {
                profile.ApplicationUser.FirstName,
                profile.ApplicationUser.LastName
            })
            .ToListAsync();

        var fullName = $"{firstName} {lastName}";

        return existingMemberNames.Any(member =>
            MemberLoginName.Matches(
                fullName,
                member.FirstName,
                member.LastName));
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

        [Required(ErrorMessage = "TC Kimlik No alanı zorunludur.")]
        [RegularExpression(
            @"^[1-9][0-9]{10}$",
            ErrorMessage = "TC Kimlik No 11 rakamdan oluşmalıdır.")]
        [TurkishNationalIdentityNumber]
        [Display(Name = "TC Kimlik No")]
        public string NationalIdentityNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = BirthDateValidation.RequiredMessage)]
        [MemberBirthDate]
        [DataType(DataType.Date)]
        [Display(Name = "Doğum tarihi")]
        public DateOnly? BirthDate { get; set; }

        [Phone]
        [Display(Name = "Telefon")]
        public string? PhoneNumber { get; set; }

        [Required(ErrorMessage = "Üyelik paketi seçmelisin.")]
        [Display(Name = "Üyelik paketi")]
        public string PackageCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "Üyelik süresi seçmelisin.")]
        [Range(1, int.MaxValue)]
        [Display(Name = "Üyelik süresi")]
        public int? ServicePackageVariantId { get; set; }

        [StringLength(160)]
        [Display(Name = "Hedef")]
        public string? FitnessGoal { get; set; }

        [Required(ErrorMessage = "Parola alanı zorunludur.")]
        [StringLength(
            100,
            ErrorMessage =
                "{0} en az {2}, en fazla {1} karakter olmalıdır.",
            MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Parola")]
        public string Password { get; set; } = string.Empty;

        [DataType(DataType.Password)]
        [Display(Name = "Parola tekrar")]
        [Compare(
            nameof(Password),
            ErrorMessage =
                "Parola ve tekrar alanı eşleşmiyor.")]
        public string ConfirmPassword { get; set; } = string.Empty;

        [StringLength(32)]
        [Display(Name = "Davet kodu")]
        public string? ReferralCode { get; set; }
    }

    public record PackageOption(
        string Code,
        string Name,
        string Audience);

    public record ServiceOption(
        int Id,
        string PackageCode,
        string Name,
        string Price,
        string PriceNote,
        string? Duration,
        string Rights,
        string? TotalPrice);

    private async Task<string> GenerateReferralCodeAsync()
    {
        var prefix = $"NO23-{Guid.NewGuid():N}"[..13].ToUpperInvariant();
        var code = prefix;
        var suffix = 0;
        while (await dbContext.MemberProfiles.AnyAsync(profile => profile.ReferralCode == code))
        {
            suffix++;
            code = $"{prefix[..Math.Min(prefix.Length, 10)]}{suffix:D2}";
        }
        return code;
    }
}
