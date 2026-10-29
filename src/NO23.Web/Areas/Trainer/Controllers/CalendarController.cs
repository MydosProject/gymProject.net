using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NO23.Web.Data;
using NO23.Web.Data.Seed;
using NO23.Web.Extensions;
using NO23.Web.Services;
using NO23.Web.ViewModels.TrainerPanel;
using System.Globalization;
using NO23.Web.Domain.Enums;
using NO23.Web.ViewModels.Admin;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace NO23.Web.Areas.Trainer.Controllers;

[Area("Trainer")]
[Authorize(Roles = ApplicationRoles.Trainer)]
[NO23.Web.Infrastructure.CalendarViewContext]
public class CalendarController(
    ApplicationDbContext dbContext,
    PersonalTrainingCalendarService calendarService) : Controller
{
    public async Task<IActionResult> Index(DateTime? week, int? memberId = null, bool includeCompleted = false, string view = "week", DateTime? date = null)
    {
        var trainerId = await GetTrainerIdAsync();
        if (trainerId is null) return Forbid();

        var selectedDate = (date ?? week ?? ClubTime.Now).Date;
        if (!ModelState.IsValid || view is not ("week" or "day") || selectedDate.Year is < 1900 or > 2100) return BadRequest();
        var daysSinceMonday = ((int)selectedDate.DayOfWeek + 6) % 7;
        var weekStart = selectedDate.AddDays(-daysSinceMonday);
        var weekEnd = weekStart.AddDays(6);
        var startUtc = ClubTime.ToUtc(weekStart);
        var endUtc = ClubTime.ToUtc(weekStart.AddDays(7));

        var sessionEntities = await dbContext.PersonalTrainingSessions.AsNoTracking()
            .Include(item => item.MemberProfile).ThenInclude(item => item.ApplicationUser)
            .Include(item => item.MemberProfile).ThenInclude(item => item.MembershipPackage)
            .Include(item => item.History)
            .Where(item => item.Status != PersonalTrainingSessionStatus.Cancelled && (includeCompleted || item.Status != PersonalTrainingSessionStatus.Completed) && item.TrainerId == trainerId && item.StartsAtUtc >= startUtc && item.StartsAtUtc < endUtc && (!memberId.HasValue || item.MemberProfileId == memberId))
            .OrderBy(item => item.StartsAtUtc)
            .AsSplitQuery()
            .ToListAsync();

        var assignedIds = await dbContext.MemberProfiles.Where(x => x.AssignedTrainerId == trainerId).Select(x => x.Id).ToListAsync();
        var creditSummaries = await new LessonCreditSummaryService(dbContext).LoadAsync(assignedIds.Concat(sessionEntities.Select(x => x.MemberProfileId)));
        var sessions = sessionEntities.Select(item => new TrainerCalendarSessionViewModel
            {
                Id = item.Id,
                MemberName = ((item.MemberProfile.ApplicationUser.FirstName ?? "") + " " +
                    (item.MemberProfile.ApplicationUser.LastName ?? "")).Trim(),
                StartsAtUtc = item.StartsAtUtc,
                DurationMinutes = item.DurationMinutes,
                Status = item.Status,
                StatusName = item.Status.GetDisplayName(),
                RemainingCredits = creditSummaries.GetValueOrDefault(item.MemberProfileId)?.Remaining ?? item.MemberProfile.RemainingClassCredits,
                IsUnlimited = item.MemberProfile.ServicePackageVariantId == null &&
                    item.MemberProfile.MembershipPackage.WeeklyClassLimit == null,
                Note = item.Note,
                CreatedAtUtc = item.CreatedAtUtc,
                History = item.History.OrderByDescending(history => history.ChangedAtUtc)
                    .Select(history => new TrainerCalendarHistoryViewModel
                    {
                        Status = history.NewStatus,
                        StatusName = history.NewStatus.GetDisplayName(),
                        PreviousStartsAtUtc = history.PreviousStartsAtUtc,
                        NewStartsAtUtc = history.NewStartsAtUtc,
                        Note = history.Note,
                        ChangedAtUtc = history.ChangedAtUtc
                    }).ToList()
            }).ToList();

        var members = await dbContext.MemberProfiles.AsNoTracking()
            .Where(item => item.AssignedTrainerId == trainerId)
            .OrderBy(item => item.ApplicationUser.FirstName).ThenBy(item => item.ApplicationUser.LastName)
            .Select(item => new TrainerAssignedMemberViewModel
            {
                Id = item.Id,
                Name = ((item.ApplicationUser.FirstName ?? "") + " " + (item.ApplicationUser.LastName ?? "")).Trim(),
                RemainingCredits = item.RemainingClassCredits,
                IsUnlimited = item.ServicePackageVariantId == null && item.MembershipPackage.WeeklyClassLimit == null,
                MembershipStartsAtUtc = item.MembershipStartsAtUtc,
                MembershipEndsAtUtc = item.MembershipEndsAtUtc
            }).ToListAsync();
        foreach (var member in members) member.RemainingCredits = creditSummaries.GetValueOrDefault(member.Id)?.Remaining ?? member.RemainingCredits;

        var groups = await dbContext.ClassSessions.AsNoTracking().Include(item => item.GroupClass)
            .Include(item => item.Reservations).Where(item => item.Status != ClassSessionStatus.Cancelled && (includeCompleted || item.Status != ClassSessionStatus.Completed) && item.GroupClass.IsActive && item.GroupClass.TrainerId == trainerId &&
                item.StartsAtUtc >= startUtc && item.StartsAtUtc < endUtc && (!memberId.HasValue ||
                    item.Reservations.Any(reservation => reservation.MemberProfileId == memberId && reservation.Status != ClassReservationStatus.Cancelled)))
            .OrderBy(item => item.StartsAtUtc).AsSplitQuery().ToListAsync();
        var appointments = await dbContext.AppointmentRequests.AsNoTracking().Include(x => x.ScheduledTrainer)
            .Where(x => !memberId.HasValue && x.ScheduledTrainerId == trainerId && x.ScheduledAtUtc >= startUtc && x.ScheduledAtUtc < endUtc &&
                (x.ScheduleStatus == AppointmentScheduleStatus.Scheduled || (includeCompleted && x.ScheduleStatus == AppointmentScheduleStatus.Completed)))
            .OrderBy(x => x.ScheduledAtUtc).ToListAsync();
        var filterMembers = await dbContext.MemberProfiles.AsNoTracking().Where(item => item.AssignedTrainerId == trainerId ||
            item.PersonalTrainingSessions.Any(session => session.TrainerId == trainerId) ||
            item.ClassReservations.Any(reservation => reservation.ClassSession.GroupClass.TrainerId == trainerId && reservation.ClassSession.GroupClass.IsActive))
            .OrderBy(item => item.ApplicationUser.FirstName).Select(item => new SelectListItem(
                item.ApplicationUser.FirstName + " " + item.ApplicationUser.LastName, item.Id.ToString())).ToListAsync();
        var turkishCulture = CultureInfo.GetCultureInfo("tr-TR");
        var days = Enumerable.Range(0, 7).Select(offset =>
        {
            var dayDate = weekStart.AddDays(offset);
            return new TrainerCalendarDayViewModel
            {
                Date = dayDate,
                DayName = turkishCulture.DateTimeFormat.GetDayName(dayDate.DayOfWeek),
                IsToday = dayDate == ClubTime.Now.Date,
                Appointments = appointments.Where(item => ClubTime.ToLocal(item.ScheduledAtUtc!.Value).Date == dayDate).ToList(),
                Groups = groups.Where(item => ClubTime.ToLocal(item.StartsAtUtc).Date == dayDate).ToList(),
                Sessions = sessions.Where(item => ClubTime.ToLocal(item.StartsAtUtc).Date == dayDate).ToList()
            };
        }).ToList();

        var model = new TrainerCalendarViewModel
        {
            SelectedDate = selectedDate, ViewMode = view,
            Credits = memberId.HasValue && filterMembers.Any(x => x.Value == memberId.Value.ToString())
                ? await new LessonCreditSummaryService(dbContext).GetAsync(memberId.Value) : null,
            Sessions = sessions, Appointments = appointments,
            Groups = groups, MemberId = memberId, IncludeCompleted = includeCompleted, FilterMembers = filterMembers,
            Members = members,
            WeekStart = weekStart,
            WeekEnd = weekEnd,
            Days = days
        };
        ViewData["CalendarNavigation"] = model.Navigation;
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PreviewWeekly(WeeklyPersonalSessionInput input)
    {
        var trainerId = await GetTrainerIdAsync();
        if (trainerId is null) return Forbid();
        return ModelState.IsValid ? Json(await calendarService.PreviewWeeklyAsync(trainerId.Value, input)) : BadRequest();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Bulk(CalendarBulkInput input)
    {
        var trainerId = await GetTrainerIdAsync();
        if (trainerId is null) return Forbid();
        var result = ModelState.IsValid ? await new CalendarBulkOperationService(dbContext).ApplyAsync(input,
            User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "", trainerId.Value) : (false, "Toplu işlem bilgilerini kontrol edin.");
        TempData[result.Item1 ? "StatusMessage" : "ErrorMessage"] = result.Item2;
        return RedirectToAction(nameof(Index), new { week = input.Week.ToString("yyyy-MM-dd"), memberId = input.MemberId, includeCompleted = input.IncludeCompleted });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateTrainerSessionViewModel model)
    {
        var trainerId = await GetTrainerIdAsync();
        if (trainerId is null) return Forbid();
        if (!ModelState.IsValid)
        {
            TempData["ErrorMessage"] = "Ders bilgilerini kontrol edin.";
            return RedirectToAction(nameof(Index), new
            {
                week = (model.Week ?? NO23.Web.Services.ClubTime.Today).ToString("yyyy-MM-dd"), includeCompleted = model.IncludeCompleted, memberId = model.FilterMemberId
            });
        }

        var result = await calendarService.CreateAsync(trainerId.Value, model.MemberProfileId,
            ClubTime.ToUtc(model.StartsAt),
            model.DurationMinutes, model.Note);
        TempData[result.Succeeded ? "StatusMessage" : "ErrorMessage"] = result.Message;
        return RedirectToAction(nameof(Index), new
        {
            week = (result.Succeeded ? model.StartsAt : model.Week ?? NO23.Web.Services.ClubTime.Today).ToString("yyyy-MM-dd"), includeCompleted = model.IncludeCompleted, memberId = model.FilterMemberId
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateWeekly(WeeklyPersonalSessionInput model)
    {
        var trainerId = await GetTrainerIdAsync();
        if (trainerId is null) return Forbid();
        var result = ModelState.IsValid
            ? await calendarService.CreateWeeklyAsync(trainerId.Value, model)
            : (false, "Haftalık birebir ders bilgilerini kontrol edin.");
        TempData[result.Item1 ? "StatusMessage" : "ErrorMessage"] = result.Item2;
        return RedirectToAction(nameof(Index), new { week = model.Week.ToString("yyyy-MM-dd"), includeCompleted = model.IncludeCompleted, memberId = model.FilterMemberId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(UpdateTrainerSessionViewModel model)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var trainerId = await GetTrainerIdAsync();
        if (trainerId is null || string.IsNullOrWhiteSpace(userId)) return Forbid();
        if (!ModelState.IsValid)
        {
            TempData["ErrorMessage"] = "Ders bilgilerini kontrol et.";
            return RedirectToAction(nameof(Index), new { week = model.Week?.ToString("yyyy-MM-dd"), includeCompleted = model.IncludeCompleted, memberId = model.FilterMemberId });
        }

        DateTime? postponedUtc = model.PostponedStartsAt is null ? null :
            ClubTime.ToUtc(model.PostponedStartsAt.Value);
        var result = await calendarService.ChangeStatusAsync(
            trainerId.Value, model.Id, model.Status, postponedUtc, userId, model.Note);
        TempData[result.Succeeded ? "StatusMessage" : "ErrorMessage"] = result.Message;
        var targetWeek = model.Status == PersonalTrainingSessionStatus.Postponed &&
            model.PostponedStartsAt is not null
                ? model.PostponedStartsAt.Value
                : model.Week ?? NO23.Web.Services.ClubTime.Today;
        return RedirectToAction(nameof(Index), new { week = targetWeek.ToString("yyyy-MM-dd"), includeCompleted = model.IncludeCompleted, memberId = model.FilterMemberId });
    }

    private async Task<int?> GetTrainerIdAsync()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return await dbContext.Trainers.Where(item => item.ApplicationUserId == userId && item.IsActive)
            .Select(item => (int?)item.Id).FirstOrDefaultAsync();
    }
}
