using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using NO23.Web.Data;
using NO23.Web.Data.Seed;
using NO23.Web.Domain.Entities;
using NO23.Web.Domain.Enums;
using NO23.Web.Extensions;
using NO23.Web.ViewModels.Admin;

namespace NO23.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = ApplicationRoles.Admin)]
public class ClassSessionsController(ApplicationDbContext dbContext) : Controller
{
    public async Task<IActionResult> Index()
    {
        var sessionRows = await dbContext.ClassSessions
            .AsNoTracking()
            .Include(session => session.GroupClass)
            .ThenInclude(groupClass => groupClass.Trainer)
            .Include(session => session.Reservations)
            .OrderBy(session => session.StartsAtUtc)
            .Select(session => new
            {
                Id = session.Id,
                ClassName = session.GroupClass.Name,
                TrainerName = session.GroupClass.Trainer.FirstName + " " + session.GroupClass.Trainer.LastName,
                StartsAtUtc = session.StartsAtUtc,
                Capacity = session.CapacityOverride ?? session.GroupClass.Capacity,
                ReservedCount = session.Reservations.Count(reservation => reservation.Status == ClassReservationStatus.Reserved),
                session.Status
            })
            .ToListAsync();

        var sessions = sessionRows
            .Select(session => new ClassSessionListItemViewModel
            {
                Id = session.Id,
                ClassName = session.ClassName,
                TrainerName = session.TrainerName,
                StartsAtUtc = session.StartsAtUtc,
                Capacity = session.Capacity,
                ReservedCount = session.ReservedCount,
                Status = session.Status.GetDisplayName(),
                IsScheduled = session.Status == ClassSessionStatus.Scheduled
            })
            .ToList();

        return View(sessions);
    }

    public async Task<IActionResult> Create()
    {
        return View(await PopulateGroupClassOptionsAsync(new ClassSessionFormViewModel
        {
            StartsAtLocal = DateTime.Now.Date.AddDays(1).AddHours(18),
            Status = ClassSessionStatus.Scheduled
        }));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ClassSessionFormViewModel model)
    {
        if (!await GroupClassExistsAsync(model.GroupClassId))
        {
            ModelState.AddModelError(nameof(model.GroupClassId), "Geçerli bir grup dersi seçmelisin.");
        }

        if (!ModelState.IsValid)
        {
            return View(await PopulateGroupClassOptionsAsync(model));
        }

        dbContext.ClassSessions.Add(MapToEntity(model));
        await dbContext.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var session = await dbContext.ClassSessions
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == id);

        if (session is null)
        {
            return NotFound();
        }

        return View(await PopulateGroupClassOptionsAsync(MapToFormModel(session)));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, ClassSessionFormViewModel model)
    {
        if (id != model.Id)
        {
            return BadRequest();
        }

        if (!await GroupClassExistsAsync(model.GroupClassId))
        {
            ModelState.AddModelError(nameof(model.GroupClassId), "Geçerli bir grup dersi seçmelisin.");
        }

        if (!ModelState.IsValid)
        {
            return View(await PopulateGroupClassOptionsAsync(model));
        }

        var session = await dbContext.ClassSessions.FindAsync(id);

        if (session is null)
        {
            return NotFound();
        }

        ApplyFormModel(session, model);
        await dbContext.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    private async Task<ClassSessionFormViewModel> PopulateGroupClassOptionsAsync(ClassSessionFormViewModel model)
    {
        model.GroupClassOptions = await dbContext.GroupClasses
            .AsNoTracking()
            .Include(groupClass => groupClass.Trainer)
            .Where(groupClass => groupClass.IsActive)
            .OrderBy(groupClass => groupClass.Name)
            .Select(groupClass => new SelectListItem(
                groupClass.Name + " - " + groupClass.Trainer.FirstName + " " + groupClass.Trainer.LastName,
                groupClass.Id.ToString()))
            .ToListAsync();

        return model;
    }

    private async Task<bool> GroupClassExistsAsync(int groupClassId)
    {
        return await dbContext.GroupClasses.AnyAsync(groupClass => groupClass.Id == groupClassId && groupClass.IsActive);
    }

    private static ClassSession MapToEntity(ClassSessionFormViewModel model)
    {
        var session = new ClassSession();
        ApplyFormModel(session, model);
        return session;
    }

    private static void ApplyFormModel(ClassSession session, ClassSessionFormViewModel model)
    {
        session.GroupClassId = model.GroupClassId;
        session.StartsAtUtc = DateTime.SpecifyKind(model.StartsAtLocal, DateTimeKind.Local).ToUniversalTime();
        session.CapacityOverride = model.CapacityOverride;
        session.Status = model.Status;
        session.UpdatedAtUtc = DateTime.UtcNow;
    }

    private static ClassSessionFormViewModel MapToFormModel(ClassSession session)
    {
        return new ClassSessionFormViewModel
        {
            Id = session.Id,
            GroupClassId = session.GroupClassId,
            StartsAtLocal = session.StartsAtUtc.ToLocalTime(),
            CapacityOverride = session.CapacityOverride,
            Status = session.Status
        };
    }
}
