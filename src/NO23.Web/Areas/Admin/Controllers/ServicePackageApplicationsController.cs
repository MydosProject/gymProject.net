using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NO23.Web.Data;
using NO23.Web.Data.Seed;
using NO23.Web.Domain.Enums;
using NO23.Web.ViewModels.Admin;

namespace NO23.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = ApplicationRoles.Admin)]
public class ServicePackageApplicationsController(ApplicationDbContext dbContext)
    : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var applications = await dbContext.ServicePackageApplications
            .AsNoTracking()
            .OrderBy(application =>
                application.Status == ServicePackageApplicationStatus.Pending
                    ? 0
                    : 1)
            .ThenByDescending(application => application.CreatedAtUtc)
            .Select(application => new
            {
                application.Id,
                application.ServicePackage.Category,
                PackageName = application.ServicePackage.Name,
                VariantName = application.ServicePackageVariant.Name,
                application.FullName,
                application.Email,
                application.PhoneNumber,
                application.Notes,
                application.Status,
                application.CreatedAtUtc
            })
            .ToListAsync();

        var model = applications.Select(application =>
            new ServicePackageApplicationListItemViewModel
            {
                Id = application.Id,
                PackageCategory = ServicePackagesController.CategoryName(
                    application.Category),
                PackageName = application.PackageName,
                VariantName = application.VariantName,
                FullName = application.FullName,
                Email = application.Email,
                PhoneNumber = application.PhoneNumber,
                Notes = application.Notes,
                Status = application.Status,
                CreatedAtLocal = DateTime.SpecifyKind(
                    application.CreatedAtUtc,
                    DateTimeKind.Utc).ToLocalTime()
            })
            .ToList();

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(
        int id,
        ServicePackageApplicationStatus status)
    {
        if (!Enum.IsDefined(typeof(ServicePackageApplicationStatus), status))
        {
            return BadRequest();
        }

        var application = await dbContext.ServicePackageApplications
            .FirstOrDefaultAsync(item => item.Id == id);

        if (application is null)
        {
            return NotFound();
        }

        application.Status = status;
        application.UpdatedAtUtc = DateTime.UtcNow;
        await dbContext.SaveChangesAsync();

        TempData["SuccessMessage"] = "Başvuru durumu güncellendi.";
        return RedirectToAction(nameof(Index));
    }
}
