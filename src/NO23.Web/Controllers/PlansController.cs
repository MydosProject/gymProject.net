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
                {Name=v.Name,Price=v.MonthlyPrice.HasValue?$"{v.MonthlyPrice:N0} ₺":$"{v.TotalPrice:N0} ₺",
                 PriceNote=v.MonthlyPrice.HasValue?"aylık":"tek seferlik",Rights=Rights(v),IsRecommended=v.IsRecommended}).ToList()
            }).ToList()
        });
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
}
