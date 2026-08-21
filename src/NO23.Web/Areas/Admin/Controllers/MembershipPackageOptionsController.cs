using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NO23.Web.Data;
using NO23.Web.Data.Seed;
using NO23.Web.Domain.Entities;
using NO23.Web.ViewModels.Admin;

namespace NO23.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = ApplicationRoles.Admin)]
public class MembershipPackageOptionsController(ApplicationDbContext dbContext) : Controller
{
    public async Task<IActionResult> Index(int? packageId = null)
    {
        var query = dbContext.MembershipPackageOptions.AsNoTracking().AsQueryable();
        if (packageId.HasValue) query = query.Where(x => x.MembershipPackageId == packageId.Value);
        var items = await query.OrderBy(x => x.MembershipPackage.DisplayOrder)
            .ThenBy(x => x.DisplayOrder).ThenBy(x => x.Name)
            .Select(x => new MembershipPackageOptionListItemViewModel
            {
                Id = x.Id, PackageName = x.MembershipPackage.Name, Name = x.Name,
                DurationDays = x.DurationDays,
                PersonalTrainingSessionCount = x.PersonalTrainingSessionCount,
                GroupClassCreditCount = x.GroupClassCreditCount,
                IncludesGymAccess = x.IncludesGymAccess, IsActive = x.IsActive,
                DisplayOrder = x.DisplayOrder, MemberCount = x.MemberProfiles.Count
            }).ToListAsync();
        return View(items);
    }

    [HttpGet]
    public async Task<IActionResult> Create(int? packageId = null)
    {
        var model = new MembershipPackageOptionFormViewModel
        {
            MembershipPackageId = packageId ?? 0, IsActive = true,
            DurationDays = 20, DisplayOrder = 10
        };
        await PopulatePackagesAsync(model);
        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(MembershipPackageOptionFormViewModel model)
    {
        await ValidateAsync(model, null);
        if (!ModelState.IsValid) { await PopulatePackagesAsync(model); return View(model); }
        dbContext.MembershipPackageOptions.Add(MapToEntity(model));
        await dbContext.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var item = await dbContext.MembershipPackageOptions.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        if (item is null) return NotFound();
        var model = MapToForm(item);
        await PopulatePackagesAsync(model);
        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, MembershipPackageOptionFormViewModel model)
    {
        if (id != model.Id) return BadRequest();
        await ValidateAsync(model, id);
        if (!ModelState.IsValid) { await PopulatePackagesAsync(model); return View(model); }
        var item = await dbContext.MembershipPackageOptions.FirstOrDefaultAsync(x => x.Id == id);
        if (item is null) return NotFound();
        Apply(item, model);
        await dbContext.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var item = await dbContext.MembershipPackageOptions.Include(x => x.MemberProfiles)
            .FirstOrDefaultAsync(x => x.Id == id);
        if (item is null) return NotFound();
        if (item.MemberProfiles.Count > 0)
        {
            TempData["ErrorMessage"] = "Üyeler tarafından seçilmiş paket seçeneği silinemez; pasif duruma getirebilirsin.";
            return RedirectToAction(nameof(Index));
        }
        dbContext.MembershipPackageOptions.Remove(item);
        await dbContext.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    private async Task ValidateAsync(MembershipPackageOptionFormViewModel model, int? id)
    {
        if (!await dbContext.MembershipPackages.AnyAsync(x => x.Id == model.MembershipPackageId))
            ModelState.AddModelError(nameof(model.MembershipPackageId), "Geçerli bir üyelik paketi seçmelisin.");
        if (!string.IsNullOrWhiteSpace(model.Name) && await dbContext.MembershipPackageOptions.AnyAsync(x =>
            x.MembershipPackageId == model.MembershipPackageId && x.Name.ToLower() == model.Name.Trim().ToLower() &&
            (!id.HasValue || x.Id != id.Value)))
            ModelState.AddModelError(nameof(model.Name), "Bu paket için aynı adlı seçenek zaten bulunuyor.");
        if (model.PersonalTrainingSessionCount == 0 && model.GroupClassCreditCount == 0 && !model.IncludesGymAccess)
            ModelState.AddModelError(string.Empty, "Seçenek en az bir PT seansı, grup dersi hakkı veya salon kullanımı içermelidir.");
        if (id.HasValue && await dbContext.MembershipPackageOptions.AnyAsync(x =>
            x.Id == id.Value && x.MembershipPackageId != model.MembershipPackageId && x.MemberProfiles.Any()))
            ModelState.AddModelError(nameof(model.MembershipPackageId),
                "Üyeler tarafından seçilmiş bir seçenek başka ana pakete taşınamaz.");
    }

    private async Task PopulatePackagesAsync(MembershipPackageOptionFormViewModel model)
    {
        model.PackageOptions = await dbContext.MembershipPackages.AsNoTracking()
            .OrderBy(x => x.DisplayOrder).Select(x => new MembershipPackageSelectOptionViewModel
            { Id = x.Id, Name = x.Name }).ToListAsync();
        if (model.MembershipPackageId == 0) model.MembershipPackageId = model.PackageOptions.FirstOrDefault()?.Id ?? 0;
    }

    private static MembershipPackageOption MapToEntity(MembershipPackageOptionFormViewModel model)
    { var item = new MembershipPackageOption(); Apply(item, model); return item; }

    private static void Apply(MembershipPackageOption item, MembershipPackageOptionFormViewModel model)
    {
        item.MembershipPackageId = model.MembershipPackageId; item.Name = model.Name.Trim();
        item.Description = model.Description.Trim(); item.DurationDays = model.DurationDays;
        item.PersonalTrainingSessionCount = model.PersonalTrainingSessionCount;
        item.GroupClassCreditCount = model.GroupClassCreditCount;
        item.IncludesGymAccess = model.IncludesGymAccess; item.IsActive = model.IsActive;
        item.DisplayOrder = model.DisplayOrder; item.UpdatedAtUtc = DateTime.UtcNow;
    }

    private static MembershipPackageOptionFormViewModel MapToForm(MembershipPackageOption item) => new()
    {
        Id = item.Id, MembershipPackageId = item.MembershipPackageId, Name = item.Name,
        Description = item.Description, DurationDays = item.DurationDays,
        PersonalTrainingSessionCount = item.PersonalTrainingSessionCount,
        GroupClassCreditCount = item.GroupClassCreditCount, IncludesGymAccess = item.IncludesGymAccess,
        IsActive = item.IsActive, DisplayOrder = item.DisplayOrder
    };
}
