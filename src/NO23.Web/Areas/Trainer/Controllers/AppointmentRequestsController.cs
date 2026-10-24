using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NO23.Web.Data;
using NO23.Web.Data.Seed;

namespace NO23.Web.Areas.Trainer.Controllers;

[Area("Trainer"), Authorize(Roles = ApplicationRoles.Trainer)]
public class AppointmentRequestsController(ApplicationDbContext db) : Controller
{
    public async Task<IActionResult> Details(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var trainerId = await db.Trainers.Where(x => userId != null && x.ApplicationUserId == userId && x.IsActive)
            .Select(x => (int?)x.Id).FirstOrDefaultAsync();
        if (trainerId is null) return Forbid();
        var request = await db.AppointmentRequests.AsNoTracking().Include(x => x.ScheduledTrainer)
            .Include(x => x.ScheduleHistory).FirstOrDefaultAsync(x => x.Id == id && x.ScheduledTrainerId == trainerId && x.ScheduledAtUtc != null);
        return request is null ? NotFound() : View(request);
    }
}
