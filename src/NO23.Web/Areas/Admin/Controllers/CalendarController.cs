using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using NO23.Web.Data;
using NO23.Web.Data.Seed;
using NO23.Web.Services;
using NO23.Web.Domain.Enums;
using NO23.Web.ViewModels.Admin;

namespace NO23.Web.Areas.Admin.Controllers;

[Area("Admin"), Authorize(Roles = ApplicationRoles.Admin)]
public class CalendarController(ApplicationDbContext db, PersonalTrainingCalendarService personal, ClassReservationService reservations) : Controller
{
    public async Task<IActionResult> Index(DateTime? week, int? trainerId, int? memberId = null, bool includeCompleted = false)
    {
        var monday = ClubTime.Monday(week ?? ClubTime.Now);
        var start = ClubTime.ToUtc(monday);
        var end = start.AddDays(7);
        return View(new WeeklyCalendarViewModel
        {
            Week = monday, TrainerId = trainerId, MemberId = memberId, IncludeCompleted = includeCompleted,
            Trainers = await db.Trainers.AsNoTracking().Where(x => x.IsActive).OrderBy(x => x.FirstName)
                .Select(x => new SelectListItem(x.FirstName + " " + x.LastName, x.Id.ToString())).ToListAsync(),
            Classes = await db.GroupClasses.AsNoTracking().Where(x => x.IsActive && x.Trainer.IsActive)
                .Select(x => new SelectListItem(x.Name + " / " + x.Trainer.FirstName + " " + x.Trainer.LastName, x.Id.ToString())).ToListAsync(),
            Members = await db.MemberProfiles.AsNoTracking().OrderBy(x => x.ApplicationUser.FirstName)
                .Select(x => new SelectListItem(x.ApplicationUser.FirstName + " " + x.ApplicationUser.LastName + " — " + x.ApplicationUser.Email, x.Id.ToString())).ToListAsync(),
            MemberPackagePeriods = await db.MemberProfiles.AsNoTracking()
                .Select(x => new MemberPackagePeriodViewModel(x.Id, x.MembershipStartsAtUtc, x.MembershipEndsAtUtc)).ToListAsync(),
            Appointments = await db.AppointmentRequests.AsNoTracking().Include(x => x.ScheduledTrainer)
                .Where(x => !memberId.HasValue && x.ScheduledAtUtc >= start && x.ScheduledAtUtc < end &&
                    (x.ScheduleStatus == AppointmentScheduleStatus.Scheduled || (includeCompleted && x.ScheduleStatus == AppointmentScheduleStatus.Completed)) &&
                    (!trainerId.HasValue || x.ScheduledTrainerId == trainerId))
                .OrderBy(x => x.ScheduledAtUtc).ToListAsync(),
            Groups = await db.ClassSessions.AsNoTracking().Include(x => x.GroupClass).ThenInclude(x => x.Trainer)
                .Include(x => x.Reservations).ThenInclude(x => x.MemberProfile).ThenInclude(x => x.ApplicationUser)
                .Where(x => x.Status != ClassSessionStatus.Cancelled && (includeCompleted || x.Status != ClassSessionStatus.Completed) && x.GroupClass.IsActive && x.StartsAtUtc >= start && x.StartsAtUtc < end && (!trainerId.HasValue || x.GroupClass.TrainerId == trainerId) &&
                    (!memberId.HasValue || x.Reservations.Any(reservation => reservation.MemberProfileId == memberId && reservation.Status != ClassReservationStatus.Cancelled)))
                .OrderBy(x => x.StartsAtUtc).AsSplitQuery().ToListAsync(),
            Personal = await db.PersonalTrainingSessions.AsNoTracking().Include(x => x.Trainer)
                .Include(x => x.MemberProfile).ThenInclude(x => x.ApplicationUser)
                .Where(x => x.Status != PersonalTrainingSessionStatus.Cancelled && (includeCompleted || x.Status != PersonalTrainingSessionStatus.Completed) && x.StartsAtUtc >= start && x.StartsAtUtc < end && (!trainerId.HasValue || x.TrainerId == trainerId) && (!memberId.HasValue || x.MemberProfileId == memberId))
                .OrderBy(x => x.StartsAtUtc).ToListAsync()
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> PreviewWeekly(AdminWeeklyPersonalSessionInput input) =>
        ModelState.IsValid ? Json(await personal.PreviewWeeklyByAdminAsync(input.TrainerId, input)) : BadRequest();

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ReviewCredits(int memberProfileId, int expectedCount, int expectedBalance,
        bool confirmed, DateTime week, bool includeCompleted = false, bool? alreadyAdjusted = null)
    {
        var result = ModelState.IsValid && alreadyAdjusted.HasValue
            ? await personal.ApplyReviewedCreditsAsync(memberProfileId, expectedCount, expectedBalance,
                confirmed, User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "", alreadyAdjusted.Value)
            : (Succeeded: false, Message: "Bakiye düzeltme yöntemini seçip bilgileri kontrol edin.");
        TempData[result.Succeeded ? "SuccessMessage" : "ErrorMessage"] = result.Message;
        return RedirectToAction(nameof(Index), new { week = week.ToString("yyyy-MM-dd"), memberId = memberProfileId, includeCompleted });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Bulk(CalendarBulkInput input)
    {
        var result = ModelState.IsValid
            ? await new CalendarBulkOperationService(db).ApplyAsync(input, User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "")
            : (false, "Toplu işlem bilgilerini kontrol edin.");
        TempData[result.Item1 ? "SuccessMessage" : "ErrorMessage"] = result.Item2;
        return RedirectToAction(nameof(Index), new { week = input.Week.ToString("yyyy-MM-dd"), trainerId = input.TrainerId, memberId = input.MemberId, includeCompleted = input.IncludeCompleted });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateGroup(WeeklyGroupInput input)
    {
        var result = ModelState.IsValid ? await new WeeklyGroupSchedulingService(db).CreateAsync(input) : (false, "Ders bilgilerini kontrol et.");
        TempData[result.Item1 ? "SuccessMessage" : "ErrorMessage"] = result.Item2;
        return RedirectToAction(nameof(Index), new { week = input.Week.ToString("yyyy-MM-dd"), trainerId = input.FilterTrainerId, includeCompleted = input.IncludeCompleted, memberId = input.FilterMemberId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CreatePersonal(AdminPersonalSessionInput input)
    {
        var result = ModelState.IsValid ? await personal.CreateByAdminAsync(input.TrainerId, input.MemberProfileId, ClubTime.ToUtc(input.StartsAt), input.DurationMinutes, input.Note)
            : (false, "Ders bilgilerini kontrol et.");
        TempData[result.Item1 ? "SuccessMessage" : "ErrorMessage"] = result.Item2;
        return RedirectToAction(nameof(Index), new { week = input.StartsAt.ToString("yyyy-MM-dd"), trainerId = input.FilterTrainerId, includeCompleted = input.IncludeCompleted, memberId = input.FilterMemberId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateWeeklyPersonal(AdminWeeklyPersonalSessionInput input)
    {
        var result = ModelState.IsValid
            ? await personal.CreateWeeklyByAdminAsync(input.TrainerId, input)
            : (false, "Haftalık birebir ders bilgilerini kontrol et.");
        TempData[result.Item1 ? "SuccessMessage" : "ErrorMessage"] = result.Item2;
        return RedirectToAction(nameof(Index), new
        {
            week = input.Week.ToString("yyyy-MM-dd"), trainerId = input.FilterTrainerId, includeCompleted = input.IncludeCompleted, memberId = input.FilterMemberId
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdatePersonal(AdminSessionUpdateInput input)
    {
        var result = ModelState.IsValid ? await personal.ChangeStatusAsync(input.TrainerId, input.Id, input.Status,
            input.PostponedStartsAt.HasValue ? ClubTime.ToUtc(input.PostponedStartsAt.Value) : null,
            User.FindFirstValue(ClaimTypes.NameIdentifier)!, input.Note) : (false, "Ders bilgilerini kontrol et.");
        TempData[result.Item1 ? "SuccessMessage" : "ErrorMessage"] = result.Item2;
        return RedirectToAction(nameof(Index), new { week = (input.Week ?? ClubTime.Now).ToString("yyyy-MM-dd"), trainerId = input.FilterTrainerId, includeCompleted = input.IncludeCompleted, memberId = input.FilterMemberId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AddParticipant(int sessionId, int memberId, DateTime? week, int? filterMemberId = null, int? filterTrainerId = null, bool includeCompleted = false)
    {
        var userId = await db.MemberProfiles.Where(x => x.Id == memberId).Select(x => x.ApplicationUserId).FirstOrDefaultAsync();
        var result = userId is null ? ReservationResult.Fail("Üye bulunamadı.") : await reservations.ReserveAsync(userId, sessionId);
        TempData[result.Succeeded ? "SuccessMessage" : "ErrorMessage"] = result.Succeeded ? "Katılımcı eklendi; ders hakkı güncellendi." : result.ErrorMessage;
        return RedirectToAction(nameof(Index), new { week = (week ?? ClubTime.Now).ToString("yyyy-MM-dd"), trainerId = filterTrainerId, memberId = filterMemberId, includeCompleted });
    }
}
