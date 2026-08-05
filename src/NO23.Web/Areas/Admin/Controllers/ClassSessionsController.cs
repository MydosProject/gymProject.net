using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using NO23.Web.Data;
using NO23.Web.Data.Seed;
using NO23.Web.Domain.Entities;
using NO23.Web.Domain.Enums;
using NO23.Web.Extensions;
using NO23.Web.Services;
using NO23.Web.ViewModels.Admin;

namespace NO23.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = ApplicationRoles.Admin)]
public class ClassSessionsController(ApplicationDbContext dbContext) : Controller
{
    public async Task<IActionResult> Index()
    {
        var nowUtc = DateTime.UtcNow;
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
                IsGroupClassActive = session.GroupClass.IsActive,
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
                IsGroupClassActive = session.IsGroupClassActive,
                StartsAtUtc = session.StartsAtUtc,
                Capacity = session.Capacity,
                ReservedCount = session.ReservedCount,
                Status = ClassSessionLifecycle
                    .GetEffectiveStatus(session.Status, session.StartsAtUtc, nowUtc)
                    .GetDisplayName(),
                IsScheduled = ClassSessionLifecycle.IsReservationOpen(
                    session.Status,
                    session.StartsAtUtc,
                    nowUtc,
                    session.IsGroupClassActive)
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

        ValidateSessionTiming(model);

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
            ModelState.AddModelError(
                nameof(model.GroupClassId),
                "Geçerli bir grup dersi seçmelisin.");
        }

        ValidateSessionTiming(model);

        if (!ModelState.IsValid)
        {
            return View(await PopulateGroupClassOptionsAsync(model));
        }

        var session = await dbContext.ClassSessions.FindAsync(id);

        if (session is null)
        {
            return NotFound();
        }

        if (model.Status == ClassSessionStatus.Cancelled &&
            session.Status != ClassSessionStatus.Cancelled)
        {
            if (session.Status != ClassSessionStatus.Scheduled)
            {
                ModelState.AddModelError(
                    nameof(model.Status),
                    "Yalnızca planlanmış ders seansları iptal edilebilir.");

                return View(await PopulateGroupClassOptionsAsync(model));
            }

            await CancelSessionAsync(session);

            return RedirectToAction(nameof(Index));
        }

        if (session.Status == ClassSessionStatus.Cancelled &&
            model.Status != ClassSessionStatus.Cancelled)
        {
            ModelState.AddModelError(
                nameof(model.Status),
                "İptal edilmiş bir seans yeniden aktif edilemez. Yeni bir seans oluşturmalısın.");

            return View(await PopulateGroupClassOptionsAsync(model));
        }

        ApplyFormModel(session, model);
        await dbContext.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id)
    {
        var session = await dbContext.ClassSessions.FindAsync(id);

        if (session is null)
        {
            return NotFound();
        }

        if (session.Status != ClassSessionStatus.Scheduled ||
            ClassSessionLifecycle.GetEffectiveStatus(
                session.Status,
                session.StartsAtUtc,
                DateTime.UtcNow) != ClassSessionStatus.Scheduled)
        {
            return RedirectToAction(nameof(Index));
        }

        await CancelSessionAsync(session);

        return RedirectToAction(nameof(Index));
    }

    private async Task CancelSessionAsync(ClassSession session)
    {
        var nowUtc = DateTime.UtcNow;

        var activeReservations = await dbContext.ClassReservations
            .Include(reservation => reservation.MemberProfile)
            .ThenInclude(profile => profile.MembershipPackage)
            .Where(reservation =>
                reservation.ClassSessionId == session.Id &&
                reservation.Status == ClassReservationStatus.Reserved)
            .ToListAsync();

        foreach (var reservation in activeReservations)
        {
            reservation.Status = ClassReservationStatus.Cancelled;
            reservation.CancelledAtUtc = nowUtc;
            reservation.CancellationReason =
                "Ders seansı yönetici tarafından iptal edildi.";

            var hasLimitedPackage =
                reservation.MemberProfile.MembershipPackage.WeeklyClassLimit is not null;

            if (hasLimitedPackage)
            {
                reservation.MemberProfile.RemainingClassCredits++;
            }

            reservation.MemberProfile.UpdatedAtUtc = nowUtc;
        }

        session.Status = ClassSessionStatus.Cancelled;
        session.UpdatedAtUtc = nowUtc;

        await dbContext.SaveChangesAsync();
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

    private void ValidateSessionTiming(ClassSessionFormViewModel model)
    {
        var startsAtUtc = DateTime.SpecifyKind(model.StartsAtLocal, DateTimeKind.Local).ToUniversalTime();

        if (model.Status == ClassSessionStatus.Scheduled && startsAtUtc <= DateTime.UtcNow)
        {
            ModelState.AddModelError(
                nameof(model.StartsAtLocal),
                "Planlanmis seans tarihi gelecekte olmali. Gecmis seans icin durumu tamamlandi secmelisin.");
        }
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
