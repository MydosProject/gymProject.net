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
public class MembershipPackagesController(ApplicationDbContext dbContext) : Controller
{
    public async Task<IActionResult> Index()
    {
        var packages = await dbContext.MembershipPackages
            .AsNoTracking()
            .OrderBy(package => package.DisplayOrder)
            .Select(package => new MembershipPackageListItemViewModel
            {
                Id = package.Id,
                Code = package.Code.ToString().ToUpper(),
                Name = package.Name,
                Audience = package.Audience,
                WeeklyClassLimit = package.WeeklyClassLimit,
                IsActive = package.IsActive,
                DisplayOrder = package.DisplayOrder,
                MemberCount = package.MemberProfiles.Count
            })
            .ToListAsync();

        return View(packages);
    }

    public IActionResult Create()
    {
        return View(new MembershipPackageFormViewModel
        {
            IsActive = true,
            DisplayOrder = 10
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(MembershipPackageFormViewModel model)
    {
        if (await PackageCodeExistsAsync(model))
        {
            ModelState.AddModelError(nameof(model.Code), "Bu paket kodu zaten kullanılıyor.");
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        dbContext.MembershipPackages.Add(MapToEntity(model));
        await dbContext.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var package = await dbContext.MembershipPackages
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
    public async Task<IActionResult> Edit(int id, MembershipPackageFormViewModel model)
    {
        if (id != model.Id)
        {
            return BadRequest();
        }

        if (await PackageCodeExistsAsync(model))
        {
            ModelState.AddModelError(nameof(model.Code), "Bu paket kodu zaten kullanılıyor.");
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var package = await dbContext.MembershipPackages.FindAsync(id);

        if (package is null)
        {
            return NotFound();
        }

        ApplyFormModel(package, model);
        await dbContext.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    private async Task<bool> PackageCodeExistsAsync(MembershipPackageFormViewModel model)
    {
        return await dbContext.MembershipPackages
            .AnyAsync(package => package.Code == model.Code && package.Id != model.Id);
    }

    private static MembershipPackage MapToEntity(MembershipPackageFormViewModel model)
    {
        var package = new MembershipPackage();
        ApplyFormModel(package, model);
        return package;
    }

    private static void ApplyFormModel(MembershipPackage package, MembershipPackageFormViewModel model)
    {
        package.Code = model.Code;
        package.Name = model.Name.Trim();
        package.Audience = model.Audience.Trim();
        package.Description = model.Description.Trim();
        package.WeeklyClassLimit = model.WeeklyClassLimit;
        package.IncludesMeasurement = model.IncludesMeasurement;
        package.IncludesBodyAnalysis = model.IncludesBodyAnalysis;
        package.IncludesNutritionSupport = model.IncludesNutritionSupport;
        package.IncludesDetailedTracking = model.IncludesDetailedTracking;
        package.IncludesMonthlyAnalysis = model.IncludesMonthlyAnalysis;
        package.IncludesPriorityReservation = model.IncludesPriorityReservation;
        package.IncludesPersonalTrainingSupport = model.IncludesPersonalTrainingSupport;
        package.IncludesKitchenBenefits = model.IncludesKitchenBenefits;
        package.IncludesPrivateEvents = model.IncludesPrivateEvents;
        package.IncludesCommunityMembership = model.IncludesCommunityMembership;
        package.IsActive = model.IsActive;
        package.DisplayOrder = model.DisplayOrder;
        package.UpdatedAtUtc = DateTime.UtcNow;
    }

    private static MembershipPackageFormViewModel MapToFormModel(MembershipPackage package)
    {
        return new MembershipPackageFormViewModel
        {
            Id = package.Id,
            Code = package.Code,
            Name = package.Name,
            Audience = package.Audience,
            Description = package.Description,
            WeeklyClassLimit = package.WeeklyClassLimit,
            IncludesMeasurement = package.IncludesMeasurement,
            IncludesBodyAnalysis = package.IncludesBodyAnalysis,
            IncludesNutritionSupport = package.IncludesNutritionSupport,
            IncludesDetailedTracking = package.IncludesDetailedTracking,
            IncludesMonthlyAnalysis = package.IncludesMonthlyAnalysis,
            IncludesPriorityReservation = package.IncludesPriorityReservation,
            IncludesPersonalTrainingSupport = package.IncludesPersonalTrainingSupport,
            IncludesKitchenBenefits = package.IncludesKitchenBenefits,
            IncludesPrivateEvents = package.IncludesPrivateEvents,
            IncludesCommunityMembership = package.IncludesCommunityMembership,
            IsActive = package.IsActive,
            DisplayOrder = package.DisplayOrder
        };
    }
}
