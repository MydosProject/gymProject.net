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
public class KitchenSubscriptionPackagesController(ApplicationDbContext dbContext) : Controller
{
    public async Task<IActionResult> Index()
    {
        var packages = await dbContext.KitchenSubscriptionPackages
            .AsNoTracking()
            .OrderBy(package => package.DisplayOrder)
            .ThenBy(package => package.Name)
            .Select(package => new KitchenSubscriptionPackageListItemViewModel
            {
                Id = package.Id,
                Plan = package.Plan.ToString(),
                Name = package.Name,
                Description = package.Description,
                Days = package.Days,
                UnitPrice = package.UnitPrice,
                IsActive = package.IsActive,
                DisplayOrder = package.DisplayOrder,
                SubscriptionCount = package.KitchenSubscriptions.Count
            })
            .ToListAsync();

        return View(packages);
    }

    public IActionResult Create()
    {
        return View(new KitchenSubscriptionPackageFormViewModel
        {
            Plan = KitchenSubscriptionPlan.FiveDays,
            Name = "Yeni Kitchen Paketi",
            Description = "NO23 Kitchen yemek paketi.",
            Days = 5,
            UnitPrice = 0,
            IsActive = true,
            DisplayOrder = 10
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(KitchenSubscriptionPackageFormViewModel model)
    {
        if (await PackagePlanExistsAsync(model))
        {
            ModelState.AddModelError(nameof(model.Plan), "Bu Kitchen planı zaten tanımlı.");
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        dbContext.KitchenSubscriptionPackages.Add(MapToEntity(model));
        await dbContext.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var package = await dbContext.KitchenSubscriptionPackages
            .AsNoTracking()
            .FirstOrDefaultAsync(package => package.Id == id);

        if (package is null)
        {
            return NotFound();
        }

        return View(MapToFormModel(package));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, KitchenSubscriptionPackageFormViewModel model)
    {
        if (id != model.Id)
        {
            return BadRequest();
        }

        if (await PackagePlanExistsAsync(model))
        {
            ModelState.AddModelError(nameof(model.Plan), "Bu Kitchen planı zaten tanımlı.");
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var package = await dbContext.KitchenSubscriptionPackages.FindAsync(id);

        if (package is null)
        {
            return NotFound();
        }

        ApplyFormModel(package, model);
        await dbContext.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var package = await dbContext.KitchenSubscriptionPackages
            .Include(item => item.KitchenSubscriptions)
            .FirstOrDefaultAsync(item => item.Id == id);

        if (package is null)
        {
            return NotFound();
        }

        if (package.KitchenSubscriptions.Count > 0)
        {
            TempData["ErrorMessage"] =
                "Bu Kitchen paketi abonelik geçmişinde kullanıldığı için silinemez. Paketi pasife alabilirsiniz.";

            return RedirectToAction(nameof(Edit), new { id });
        }

        dbContext.KitchenSubscriptionPackages.Remove(package);
        await dbContext.SaveChangesAsync();

        TempData["SuccessMessage"] = "Kitchen paketi başarıyla silindi.";

        return RedirectToAction(nameof(Index));
    }

    private async Task<bool> PackagePlanExistsAsync(KitchenSubscriptionPackageFormViewModel model)
    {
        return await dbContext.KitchenSubscriptionPackages
            .AnyAsync(package => package.Plan == model.Plan && package.Id != model.Id);
    }

    private static KitchenSubscriptionPackage MapToEntity(KitchenSubscriptionPackageFormViewModel model)
    {
        var package = new KitchenSubscriptionPackage();
        ApplyFormModel(package, model);
        return package;
    }

    private static void ApplyFormModel(
        KitchenSubscriptionPackage package,
        KitchenSubscriptionPackageFormViewModel model)
    {
        package.Plan = model.Plan;
        package.Name = model.Name.Trim();
        package.Description = model.Description.Trim();
        package.Days = model.Days;
        package.UnitPrice = model.UnitPrice;
        package.IsActive = model.IsActive;
        package.DisplayOrder = model.DisplayOrder;
        package.UpdatedAtUtc = DateTime.UtcNow;
    }

    private static KitchenSubscriptionPackageFormViewModel MapToFormModel(
        KitchenSubscriptionPackage package)
    {
        return new KitchenSubscriptionPackageFormViewModel
        {
            Id = package.Id,
            Plan = package.Plan,
            Name = package.Name,
            Description = package.Description,
            Days = package.Days,
            UnitPrice = package.UnitPrice,
            IsActive = package.IsActive,
            DisplayOrder = package.DisplayOrder
        };
    }
}