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
    public async Task<IActionResult> Index(int page = 1, string? query = null, string? state = null)
    {
        query = query?.Trim();
        if (!ModelState.IsValid || query?.Length > 80) return BadRequest();
        var source = dbContext.ServicePackageApplications.AsNoTracking().AsQueryable();
        ViewBag.OverallCount = await source.CountAsync();
        ViewBag.PendingCount = await source.CountAsync(x => x.Status == ServicePackageApplicationStatus.Pending);
        if (!string.IsNullOrEmpty(state))
        {
            if (!Enum.TryParse<ServicePackageApplicationStatus>(state, true, out var status) || !Enum.IsDefined(status)) return BadRequest();
            source = source.Where(x => x.Status == status);
            state = status.ToString();
        }
        if (!string.IsNullOrEmpty(query))
        {
            var term = query.ToLower(System.Globalization.CultureInfo.GetCultureInfo("tr-TR"));
            int.TryParse(query.TrimStart('#'), out var id);
            source = source.Where(x => x.Id == id || x.FullName.Replace("I", "ı").Replace("İ", "i").ToLower().Contains(term) ||
                x.Email.ToLower().Contains(term) || x.PhoneNumber.Contains(query));
        }
        var navigation = new NO23.Web.ViewModels.ListNavigationViewModel(await source.CountAsync(), page, query, state,
            Enum.GetValues<ServicePackageApplicationStatus>().Select(x => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem(NO23.Web.Extensions.EnumDisplayNameExtensions.GetDisplayName(x), x.ToString())).ToArray());
        ViewData["ListNavigation"] = navigation;
        var applications = await source
            .OrderBy(application =>
                application.Status == ServicePackageApplicationStatus.Pending
                    ? 0
                    : 1)
            .ThenByDescending(application => application.CreatedAtUtc)
            .ThenByDescending(application => application.Id)
            .Skip(navigation.Skip).Take(NO23.Web.ViewModels.ListNavigationViewModel.PageSize)
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
                application.FamilyCode,
                application.SiblingDiscountPercent,
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
                FamilyCode = application.FamilyCode,
                SiblingDiscountPercent = application.SiblingDiscountPercent,
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
        ServicePackageApplicationStatus status, int page = 1, string? query = null, string? state = null)
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
        return RedirectToAction(nameof(Index), new { page, query, state });
    }
}
