using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NO23.Web.Data;
using NO23.Web.Data.Seed;
using NO23.Web.Domain.Entities;
using NO23.Web.Domain.Enums;
using NO23.Web.ViewModels.Admin;

namespace NO23.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = ApplicationRoles.Admin)]
public class ServicePackagesController(ApplicationDbContext dbContext) : Controller
{
    public async Task<IActionResult> Index()
    {
        var items = await dbContext.ServicePackages.AsNoTracking()
            .OrderBy(x => x.Category).ThenBy(x => x.DisplayOrder)
            .Select(x => new { x.Id, x.Category, x.Name, x.Subtitle, x.IsFeatured,
                x.IsActive, x.DisplayOrder, VariantCount = x.Variants.Count }).ToListAsync();
        return View(items.Select(x => new ServicePackageListItemViewModel
        { Id=x.Id,Category=CategoryName(x.Category),Name=x.Name,Subtitle=x.Subtitle,
          IsFeatured=x.IsFeatured,IsActive=x.IsActive,DisplayOrder=x.DisplayOrder,VariantCount=x.VariantCount }).ToList());
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    { var model = new ServicePackageFormViewModel { IsActive = true, DisplayOrder = 10 }; await PopulateAsync(model); return View(model); }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ServicePackageFormViewModel model)
    {
        await ValidatePackageAsync(model, null);
        if (!ModelState.IsValid) { await PopulateAsync(model); return View(model); }
        var item = new ServicePackage(); Apply(item, model); ApplyFeatures(item, model.FeaturesText);
        dbContext.ServicePackages.Add(item); await dbContext.SaveChangesAsync();
        return RedirectToAction(nameof(Edit), new { id = item.Id });
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var item = await dbContext.ServicePackages.AsNoTracking().Include(x => x.Features)
            .FirstOrDefaultAsync(x => x.Id == id); if (item is null) return NotFound();
        var model = new ServicePackageFormViewModel
        { Id = item.Id, Category = item.Category, Slug = item.Slug, Name = item.Name,
          Subtitle = item.Subtitle, Description = item.Description, MembershipPackageId = item.MembershipPackageId,
          IsFeatured = item.IsFeatured, IsActive = item.IsActive, DisplayOrder = item.DisplayOrder,
          FeaturesText = string.Join(Environment.NewLine, item.Features.OrderBy(x => x.DisplayOrder).Select(x => x.Text)) };
        await PopulateAsync(model); return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, ServicePackageFormViewModel model)
    {
        if (id != model.Id) return BadRequest(); await ValidatePackageAsync(model, id);
        if (!ModelState.IsValid) { await PopulateAsync(model); return View(model); }
        var item = await dbContext.ServicePackages.Include(x => x.Features).FirstOrDefaultAsync(x => x.Id == id);
        if (item is null) return NotFound(); Apply(item, model);
        dbContext.ServicePackageFeatures.RemoveRange(item.Features); item.Features.Clear(); ApplyFeatures(item, model.FeaturesText);
        await dbContext.SaveChangesAsync(); return RedirectToAction(nameof(Edit), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var item = await dbContext.ServicePackages.FindAsync(id); if (item is null) return NotFound();
        dbContext.ServicePackages.Remove(item); await dbContext.SaveChangesAsync(); return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> CreateVariant(int packageId)
    {
        var name = await dbContext.ServicePackages.Where(x => x.Id == packageId).Select(x => x.Name).FirstOrDefaultAsync();
        if (name is null) return NotFound(); return View("Variant", new ServicePackageVariantFormViewModel
        { ServicePackageId = packageId, PackageName = name, BillingType = ServicePackageBillingType.OneTime, IsActive = true, DisplayOrder = 10 });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateVariant(ServicePackageVariantFormViewModel model)
    {
        await ValidateVariantAsync(model, null); if (!ModelState.IsValid) { await SetPackageNameAsync(model); return View("Variant", model); }
        var item = new ServicePackageVariant(); Apply(item, model); dbContext.ServicePackageVariants.Add(item);
        await dbContext.SaveChangesAsync(); return RedirectToAction(nameof(Edit), new { id = model.ServicePackageId });
    }

    [HttpGet]
    public async Task<IActionResult> EditVariant(int id)
    {
        var item = await dbContext.ServicePackageVariants.AsNoTracking().Include(x => x.ServicePackage).FirstOrDefaultAsync(x => x.Id == id);
        if (item is null) return NotFound(); return View("Variant", Map(item));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> EditVariant(int id, ServicePackageVariantFormViewModel model)
    {
        if (id != model.Id) return BadRequest(); await ValidateVariantAsync(model, id);
        if (!ModelState.IsValid) { await SetPackageNameAsync(model); return View("Variant", model); }
        var item = await dbContext.ServicePackageVariants.FirstOrDefaultAsync(x => x.Id == id); if (item is null) return NotFound();
        Apply(item, model); await dbContext.SaveChangesAsync(); return RedirectToAction(nameof(Edit), new { id = model.ServicePackageId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteVariant(int id)
    {
        var item = await dbContext.ServicePackageVariants.FindAsync(id); if (item is null) return NotFound();
        var packageId = item.ServicePackageId; dbContext.ServicePackageVariants.Remove(item); await dbContext.SaveChangesAsync();
        return RedirectToAction(nameof(Edit), new { id = packageId });
    }

    private async Task PopulateAsync(ServicePackageFormViewModel model)
    {
        model.MembershipPackages = await dbContext.MembershipPackages.AsNoTracking().OrderBy(x => x.DisplayOrder)
            .Select(x => new MembershipPackageSelectOptionViewModel { Id = x.Id, Name = x.Name }).ToListAsync();
        if (model.Id > 0)
        {
            var variants = await dbContext.ServicePackageVariants.AsNoTracking().Where(x => x.ServicePackageId == model.Id)
                .OrderBy(x => x.DisplayOrder).ToListAsync();
            model.Variants = variants.Select(x => new ServicePackageVariantListItemViewModel
            { Id = x.Id, Name = x.Name, Price = x.MonthlyPrice.HasValue ? $"{x.MonthlyPrice:N0} ₺ / ay" : $"{x.TotalPrice:N0} ₺",
              Rights = Rights(x), IsRecommended = x.IsRecommended, IsActive = x.IsActive }).ToList();
        }
    }

    private async Task ValidatePackageAsync(ServicePackageFormViewModel model, int? id)
    {
        if (!string.IsNullOrWhiteSpace(model.Slug) && await dbContext.ServicePackages.AnyAsync(x => x.Slug == model.Slug.Trim().ToLower() && (!id.HasValue || x.Id != id)))
            ModelState.AddModelError(nameof(model.Slug), "Bu URL kodu kullanılıyor.");
        if (model.Category == ServicePackageCategory.Membership && !model.MembershipPackageId.HasValue)
            ModelState.AddModelError(nameof(model.MembershipPackageId), "Membership paketleri bir üyelik seviyesine bağlanmalıdır.");
    }

    private async Task ValidateVariantAsync(ServicePackageVariantFormViewModel model, int? id)
    {
        if (!await dbContext.ServicePackages.AnyAsync(x => x.Id == model.ServicePackageId)) ModelState.AddModelError(string.Empty, "Paket bulunamadı.");
        if (await dbContext.ServicePackageVariants.AnyAsync(x => x.ServicePackageId == model.ServicePackageId && x.Name == model.Name && (!id.HasValue || x.Id != id)))
            ModelState.AddModelError(nameof(model.Name), "Aynı adlı varyant zaten var.");
        if (model.BillingType == ServicePackageBillingType.MonthlySubscription && !model.MonthlyPrice.HasValue)
            ModelState.AddModelError(nameof(model.MonthlyPrice), "Aylık abonelikte aylık fiyat zorunludur.");
    }

    private async Task SetPackageNameAsync(ServicePackageVariantFormViewModel model) => model.PackageName =
        await dbContext.ServicePackages.Where(x => x.Id == model.ServicePackageId).Select(x => x.Name).FirstOrDefaultAsync() ?? string.Empty;
    private static void Apply(ServicePackage x, ServicePackageFormViewModel m)
    { x.Category=m.Category; x.Slug=m.Slug.Trim().ToLower(); x.Name=m.Name.Trim(); x.Subtitle=m.Subtitle.Trim(); x.Description=m.Description.Trim(); x.MembershipPackageId=m.Category==ServicePackageCategory.Membership?m.MembershipPackageId:null; x.IsFeatured=m.IsFeatured; x.IsActive=m.IsActive; x.DisplayOrder=m.DisplayOrder; x.UpdatedAtUtc=DateTime.UtcNow; }
    private static void ApplyFeatures(ServicePackage x, string text) { var lines=text.Split(['\r','\n'],StringSplitOptions.TrimEntries|StringSplitOptions.RemoveEmptyEntries).Distinct(); var i=1; foreach(var line in lines)x.Features.Add(new ServicePackageFeature{Text=line,DisplayOrder=i++}); }
    private static void Apply(ServicePackageVariant x, ServicePackageVariantFormViewModel m)
    { x.ServicePackageId=m.ServicePackageId;x.Name=m.Name.Trim();x.BillingType=m.BillingType;x.DurationMonths=m.DurationMonths;x.DurationDays=m.DurationDays;x.MonthlyPrice=m.MonthlyPrice;x.TotalPrice=m.TotalPrice;x.PersonalTrainingSessionCount=m.PersonalTrainingSessionCount;x.ReformerClassCreditCount=m.ReformerClassCreditCount;x.PerformanceClassCreditCount=m.PerformanceClassCreditCount;x.GroupClassCreditCount=m.GroupClassCreditCount;x.KidsClassCreditCount=m.KidsClassCreditCount;x.IncludesGymAccess=m.IncludesGymAccess;x.IsRecommended=m.IsRecommended;x.IsActive=m.IsActive;x.DisplayOrder=m.DisplayOrder;x.UpdatedAtUtc=DateTime.UtcNow; }
    private static ServicePackageVariantFormViewModel Map(ServicePackageVariant x)=>new(){Id=x.Id,ServicePackageId=x.ServicePackageId,PackageName=x.ServicePackage.Name,Name=x.Name,BillingType=x.BillingType,DurationMonths=x.DurationMonths,DurationDays=x.DurationDays,MonthlyPrice=x.MonthlyPrice,TotalPrice=x.TotalPrice,PersonalTrainingSessionCount=x.PersonalTrainingSessionCount,ReformerClassCreditCount=x.ReformerClassCreditCount,PerformanceClassCreditCount=x.PerformanceClassCreditCount,GroupClassCreditCount=x.GroupClassCreditCount,KidsClassCreditCount=x.KidsClassCreditCount,IncludesGymAccess=x.IncludesGymAccess,IsRecommended=x.IsRecommended,IsActive=x.IsActive,DisplayOrder=x.DisplayOrder};
    private static string Rights(ServicePackageVariant x)=>string.Join(" · ",new[]{x.PersonalTrainingSessionCount>0?$"{x.PersonalTrainingSessionCount} PT":null,x.ReformerClassCreditCount>0?$"{x.ReformerClassCreditCount} Reformer":null,x.PerformanceClassCreditCount>0?$"{x.PerformanceClassCreditCount} Performance":null,x.GroupClassCreditCount>0?$"{x.GroupClassCreditCount} Grup":null,x.KidsClassCreditCount>0?$"{x.KidsClassCreditCount} Kids":null}.Where(x=>x!=null));
    public static string CategoryName(ServicePackageCategory value)=>value switch{ServicePackageCategory.Membership=>"Membership",ServicePackageCategory.PersonalTraining=>"Personal Training",ServicePackageCategory.GroupClasses=>"Grup Dersleri",ServicePackageCategory.KidsClub=>"Kids Club",_=>value.ToString()};
}
