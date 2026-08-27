using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NO23.Web.Data;
using NO23.Web.Domain.Entities;
using NO23.Web.Domain.Enums;
using NO23.Web.ViewModels.Plans;

namespace NO23.Web.Controllers;

[AllowAnonymous]
[Route("plans")]
public class PlansController(ApplicationDbContext dbContext) : Controller
{
    [HttpGet("{category}")]
    public async Task<IActionResult> Index(string category)
    {
        if (!TryCategory(category, out var value)) return NotFound();
        var rawPackages = await dbContext.ServicePackages.AsNoTracking()
            .Where(x => x.Category == value && x.IsActive)
            .OrderBy(x => x.DisplayOrder).ThenBy(x => x.Name)
            .Select(x => new
            {
                x.Slug, x.Name, x.Subtitle, x.Description, x.IsFeatured,
                MembershipCode = x.MembershipPackage != null ? x.MembershipPackage.Code : (MembershipPackageCode?)null,
                Features = x.Features.OrderBy(f => f.DisplayOrder).Select(f => f.Text).ToList(),
                Variants = x.Variants.Where(v => v.IsActive).OrderBy(v => v.DisplayOrder).ToList()
            }).ToListAsync();
        var meta = Meta(value);
        return View(new ServicePackageCatalogViewModel
        {
            Category = value, CategoryTitle = meta.Title, Headline = meta.Headline, Description = meta.Description,
            Packages = rawPackages.Select(x => new ServicePackageCardViewModel
            {
                Slug=x.Slug,Name=x.Name,Subtitle=x.Subtitle,Description=x.Description,IsFeatured=x.IsFeatured,
                MembershipCode=x.MembershipCode?.ToString().ToUpperInvariant(),Features=x.Features,
                Variants=x.Variants.Select(v=>new ServicePackageVariantCardViewModel
                {Id=v.Id,Name=v.Name,Price=v.MonthlyPrice.HasValue?$"{v.MonthlyPrice:N0} ₺":$"{v.TotalPrice:N0} ₺",
                 PriceNote=v.MonthlyPrice.HasValue?"aylık":"tek seferlik",Rights=Rights(v),IsRecommended=v.IsRecommended}).ToList()
            }).ToList()
        });
    }

    [HttpGet("apply")]
    public async Task<IActionResult> Apply(
        string package,
        int variant)
    {
        var selection = await LoadApplicationSelectionAsync(
            package,
            variant);

        if (selection is null)
        {
            return NotFound();
        }

        var input = new PlanApplicationInputViewModel
        {
            ServicePackageId = selection.PackageId,
            ServicePackageVariantId = selection.VariantId
        };

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!string.IsNullOrWhiteSpace(userId))
        {
            var user = await dbContext.Users
                .AsNoTracking()
                .Where(item => item.Id == userId)
                .Select(item => new
                {
                    item.FirstName,
                    item.LastName,
                    item.Email,
                    item.PhoneNumber
                })
                .FirstOrDefaultAsync();

            if (user is not null)
            {
                input.FullName =
                    $"{user.FirstName} {user.LastName}".Trim();
                input.Email = user.Email ?? string.Empty;
                input.PhoneNumber = user.PhoneNumber ?? string.Empty;
            }
        }

        return View(BuildApplicationPage(selection, input));
    }

    [HttpPost("apply")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Apply(
        PlanApplicationInputViewModel input)
    {
        var selection = await LoadApplicationSelectionAsync(
            input.ServicePackageId,
            input.ServicePackageVariantId);

        if (selection is null)
        {
            ModelState.AddModelError(
                string.Empty,
                "Seçtiğin paket veya ders seçeneği artık kullanılamıyor.");

            return BadRequest(ModelState);
        }

        if (!ModelState.IsValid)
        {
            return View(BuildApplicationPage(selection, input));
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var normalizedEmail = input.Email.Trim().ToUpperInvariant();
        var duplicateThreshold = DateTime.UtcNow.AddMinutes(-5);
        var isDuplicate = await dbContext.ServicePackageApplications
            .AsNoTracking()
            .AnyAsync(application =>
                application.ServicePackageVariantId == selection.VariantId &&
                application.Email.ToUpper() == normalizedEmail &&
                application.CreatedAtUtc >= duplicateThreshold);

        if (!isDuplicate)
        {
            dbContext.ServicePackageApplications.Add(
                new ServicePackageApplication
                {
                    ServicePackageId = selection.PackageId,
                    ServicePackageVariantId = selection.VariantId,
                    ApplicationUserId = userId,
                    FullName = input.FullName.Trim(),
                    Email = input.Email.Trim(),
                    PhoneNumber = input.PhoneNumber.Trim(),
                    Notes = string.IsNullOrWhiteSpace(input.Notes)
                        ? null
                        : input.Notes.Trim()
                });

            await dbContext.SaveChangesAsync();
        }

        TempData["PlanApplicationPackage"] = selection.PackageName;
        TempData["PlanApplicationVariant"] = selection.VariantName;

        return RedirectToAction(nameof(ApplicationReceived));
    }

    [HttpGet("application-received")]
    public IActionResult ApplicationReceived()
    {
        ViewData["PackageName"] =
            TempData["PlanApplicationPackage"]?.ToString();
        ViewData["VariantName"] =
            TempData["PlanApplicationVariant"]?.ToString();

        return View();
    }

    private static bool TryCategory(string text, out ServicePackageCategory value)
    {
        value = text.ToLowerInvariant() switch
        { "membership"=>ServicePackageCategory.Membership,"personal-training" or "pt"=>ServicePackageCategory.PersonalTraining,
          "group-classes" or "grup-dersleri"=>ServicePackageCategory.GroupClasses,"kids-club"=>ServicePackageCategory.KidsClub,_=>0 };
        return value != 0;
    }
    private static (string Title,string Headline,string Description) Meta(ServicePackageCategory value)=>value switch
    {
        ServicePackageCategory.Membership=>("Membership","Hedefine uygun üyelik düzenini seç.","PT, grup dersleri, salon erişimi ve gelişim takibini bir araya getiren üyelik seviyeleri."),
        ServicePackageCategory.PersonalTraining=>("Personal Training","Birebir çalış, gelişimini hızlandır.","Hedefine ve çalışma ritmine uygun PT ders paketini belirle."),
        ServicePackageCategory.GroupClasses=>("Grup Dersleri","Dersini seç, ritmini yakala.","Reformer ve performans grup derslerini ihtiyacına uygun ders haklarıyla planla."),
        ServicePackageCategory.KidsClub=>("Kids Club","Hareketi doğru temelle başlat.","Yaşa uygun, güvenli ve gelişim odaklı çocuk programları."),_=>throw new ArgumentOutOfRangeException()
    };
    private static string Rights(ServicePackageVariant x)=>string.Join(" · ",new[]{x.PersonalTrainingSessionCount>0?$"{x.PersonalTrainingSessionCount} PT":null,x.ReformerClassCreditCount>0?$"{x.ReformerClassCreditCount} Reformer":null,x.PerformanceClassCreditCount>0?$"{x.PerformanceClassCreditCount} Performance":null,x.GroupClassCreditCount>0?$"{x.GroupClassCreditCount} Grup":null,x.KidsClassCreditCount>0?$"{x.KidsClassCreditCount} Ders":null,x.IncludesGymAccess?"Salon erişimi":null}.Where(x=>x!=null));

    private async Task<ApplicationSelection?>
        LoadApplicationSelectionAsync(
            string packageSlug,
            int variantId)
    {
        var variant = await dbContext.ServicePackageVariants
            .AsNoTracking()
            .Include(item => item.ServicePackage)
            .Where(item =>
                item.Id == variantId &&
                item.IsActive &&
                item.ServicePackage.Slug == packageSlug &&
                item.ServicePackage.IsActive)
            .FirstOrDefaultAsync();

        return variant is null
            ? null
            : MapApplicationSelection(variant);
    }

    private async Task<ApplicationSelection?>
        LoadApplicationSelectionAsync(
            int packageId,
            int variantId)
    {
        var variant = await dbContext.ServicePackageVariants
            .AsNoTracking()
            .Include(item => item.ServicePackage)
            .Where(item =>
                item.Id == variantId &&
                item.IsActive &&
                item.ServicePackageId == packageId &&
                item.ServicePackage.IsActive)
            .FirstOrDefaultAsync();

        return variant is null
            ? null
            : MapApplicationSelection(variant);
    }

    private static ApplicationSelection MapApplicationSelection(
        ServicePackageVariant variant) => new(
            variant.ServicePackageId,
            variant.Id,
            variant.ServicePackage.Category,
            variant.ServicePackage.Name,
            variant.Name,
            variant.MonthlyPrice,
            variant.TotalPrice,
            Rights(variant));

    private static PlanApplicationPageViewModel BuildApplicationPage(
        ApplicationSelection selection,
        PlanApplicationInputViewModel input) => new()
    {
        PackageName = selection.PackageName,
        PackageCategory = Meta(selection.Category).Title,
        CategoryRoute = CategoryRoute(selection.Category),
        VariantName = selection.VariantName,
        VariantPrice = selection.MonthlyPrice.HasValue
            ? $"{selection.MonthlyPrice:N0} ₺ / ay"
            : $"{selection.TotalPrice:N0} ₺",
        VariantRights = selection.Rights,
        Input = input
    };

    private static string CategoryRoute(
        ServicePackageCategory category) => category switch
    {
        ServicePackageCategory.Membership => "membership",
        ServicePackageCategory.PersonalTraining => "personal-training",
        ServicePackageCategory.GroupClasses => "group-classes",
        ServicePackageCategory.KidsClub => "kids-club",
        _ => "membership"
    };

    private sealed record ApplicationSelection(
        int PackageId,
        int VariantId,
        ServicePackageCategory Category,
        string PackageName,
        string VariantName,
        decimal? MonthlyPrice,
        decimal TotalPrice,
        string Rights);
}
