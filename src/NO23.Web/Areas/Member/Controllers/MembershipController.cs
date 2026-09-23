using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NO23.Web.Data;
using NO23.Web.Data.Seed;
using NO23.Web.Domain.Enums;
using NO23.Web.Services;
using NO23.Web.Services.Payments;
using NO23.Web.ViewModels.Member;

namespace NO23.Web.Areas.Member.Controllers;

[Area("Member")]
[Authorize(Roles = ApplicationRoles.Member)]
public sealed class MembershipController(
    ApplicationDbContext dbContext,
    MembershipRenewalService renewalService,
    IyzicoPaymentService paymentService,
    IOptions<IyzicoOptions> paymentOptions) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(string? payment)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId)) return Challenge();
        if (payment == "success") TempData["SuccessMessage"] = "Ödeme alındı; üyelik bilgileriniz güncellendi.";
        if (payment == "failed") TempData["ErrorMessage"] = "Ödeme doğrulanamadı. Siparişlerinizi kontrol edin.";
        return View(await BuildModelAsync(userId, new MembershipRenewalInputViewModel()));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Buy(MembershipRenewalInputViewModel input)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId)) return Challenge();
        if (!paymentOptions.Value.Enabled)
            ModelState.AddModelError(string.Empty, "Çevrimiçi ödeme şu anda kullanılamıyor.");
        if (!ModelState.IsValid)
            return View("Index", await BuildModelAsync(userId, input));

        var result = await renewalService.CreateOrderAsync(userId, input.VariantId,
            input.FullName, input.Phone, input.BillingAddress, input.District, input.City);
        if (result.OrderId is null)
        {
            ModelState.AddModelError(string.Empty, result.Error ?? "Sipariş oluşturulamadı.");
            return View("Index", await BuildModelAsync(userId, input));
        }

        var returnUrl = Url.Action(nameof(Index), "Membership", new { area = "Member" }, Request.Scheme);
        var paymentStart = await paymentService.InitializeAsync(result.OrderId.Value,
            HttpContext.Connection.RemoteIpAddress?.ToString(), returnUrl);
        if (!paymentStart.Succeeded || string.IsNullOrWhiteSpace(paymentStart.RedirectUrl))
        {
            TempData["ErrorMessage"] = paymentStart.ErrorMessage ?? "Ödeme başlatılamadı.";
            return RedirectToAction(nameof(Index));
        }
        return Redirect(paymentStart.RedirectUrl);
    }

    private async Task<MembershipRenewalViewModel> BuildModelAsync(
        string userId, MembershipRenewalInputViewModel input)
    {
        var profile = await dbContext.MemberProfiles.AsNoTracking()
            .Include(x => x.ApplicationUser)
            .Include(x => x.ServicePackageVariant)
                .ThenInclude(x => x!.ServicePackage)
            .Include(x => x.MembershipPackage)
            .FirstOrDefaultAsync(x => x.ApplicationUserId == userId);

        var variants = await dbContext.ServicePackageVariants.AsNoTracking()
            .Include(x => x.ServicePackage)
            .Where(x => x.IsActive && x.ServicePackage.IsActive &&
                x.ServicePackage.Category == ServicePackageCategory.Membership &&
                x.ServicePackage.MembershipPackageId != null &&
                !x.PriceOnRequest && x.TotalPrice > 0)
            .OrderBy(x => x.ServicePackage.DisplayOrder)
            .ThenBy(x => x.DisplayOrder)
            .ToListAsync();

        if (string.IsNullOrWhiteSpace(input.FullName) && profile is not null)
            input.FullName = $"{profile.ApplicationUser.FirstName} {profile.ApplicationUser.LastName}".Trim();
        if (string.IsNullOrWhiteSpace(input.Phone) && profile is not null)
            input.Phone = profile.ApplicationUser.PhoneNumber ?? string.Empty;

        return new MembershipRenewalViewModel
        {
            CurrentPackageName = profile?.ServicePackageVariant is { } current
                ? $"{current.ServicePackage.Name} - {current.Name}"
                : profile?.MembershipPackage.Name ?? "Paket bulunamadı",
            MembershipEndsAtUtc = profile?.MembershipEndsAtUtc,
            PaymentEnabled = paymentOptions.Value.Enabled,
            Input = input,
            Options = variants.Select(x => new MembershipRenewalOptionViewModel
            {
                Id = x.Id,
                PackageName = x.ServicePackage.Name,
                VariantName = x.Name,
                TotalPrice = x.TotalPrice,
                DurationMonths = x.DurationMonths ?? 1,
                DurationDays = x.DurationDays,
                TotalCredits = MemberPackageEntitlement.CalculateInitialCredits(x)
            }).ToList()
        };
    }
}
