using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NO23.Web.Data;
using NO23.Web.Data.Seed;
using NO23.Web.Domain.Enums;

namespace NO23.Web.Areas.Admin.Controllers;

[Area("Admin"), Authorize(Roles = ApplicationRoles.Admin)]
public class AppointmentRequestsController(ApplicationDbContext dbContext) : Controller
{
    public async Task<IActionResult> Index() => View(await dbContext.AppointmentRequests.AsNoTracking()
        .OrderBy(x => x.Status != ServicePackageApplicationStatus.Pending).ThenByDescending(x => x.CreatedAtUtc).ToListAsync());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(int id, ServicePackageApplicationStatus status)
    {
        if (!Enum.IsDefined(status)) return BadRequest();
        var request = await dbContext.AppointmentRequests.FindAsync(id);
        if (request is null) return NotFound();
        request.Status = status;
        request.UpdatedAtUtc = DateTime.UtcNow;
        await dbContext.SaveChangesAsync();
        TempData["SuccessMessage"] = "Randevu talebinin durumu güncellendi.";
        return RedirectToAction(nameof(Index));
    }
}
