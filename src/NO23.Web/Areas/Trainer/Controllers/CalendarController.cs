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

namespace NO23.Web.Areas.Trainer.Controllers;

[Area("Trainer")]
[Authorize(Roles = ApplicationRoles.Trainer)]
public class CalendarController(
    ApplicationDbContext dbContext,
    PersonalTrainingCalendarService calendarService) : Controller
{
    public async Task<IActionResult> Index(DateTime? week)
    {
        var trainerId = await GetTrainerIdAsync();
        if (trainerId is null) return Forbid();

        var selectedDate = (week ?? DateTime.Today).Date;
        var daysSinceMonday = ((int)selectedDate.DayOfWeek + 6) % 7;
        var weekStart = selectedDate.AddDays(-daysSinceMonday);
        var weekEnd = weekStart.AddDays(6);
        var startUtc = DateTime.SpecifyKind(weekStart, DateTimeKind.Local).ToUniversalTime();
        var endUtc = DateTime.SpecifyKind(weekStart.AddDays(7), DateTimeKind.Local).ToUniversalTime();

        var sessionEntities = await dbContext.PersonalTrainingSessions.AsNoTracking()
            .Include(item => item.MemberProfile).ThenInclude(item => item.ApplicationUser)
            .Include(item => item.MemberProfile).ThenInclude(item => item.MembershipPackage)
            .Include(item => item.History)
            .Where(item => item.TrainerId == trainerId && item.StartsAtUtc >= startUtc && item.StartsAtUtc < endUtc)
            .OrderBy(item => item.StartsAtUtc)
            .AsSplitQuery()
            .ToListAsync();

        var sessions = sessionEntities.Select(item => new TrainerCalendarSessionViewModel
            {
                Id = item.Id,
                MemberName = ((item.MemberProfile.ApplicationUser.FirstName ?? "") + " " +
                    (item.MemberProfile.ApplicationUser.LastName ?? "")).Trim(),
                StartsAtUtc = item.StartsAtUtc,
                DurationMinutes = item.DurationMinutes,
                Status = item.Status,
                StatusName = item.Status.GetDisplayName(),
                RemainingCredits = item.MemberProfile.RemainingClassCredits,
                IsUnlimited = item.MemberProfile.MembershipPackage.WeeklyClassLimit == null,
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
                RemainingCredits = item.RemainingClassCredits
                ,IsUnlimited = item.MembershipPackage.WeeklyClassLimit == null
            }).ToListAsync();

        var turkishCulture = CultureInfo.GetCultureInfo("tr-TR");
        var days = Enumerable.Range(0, 7).Select(offset =>
        {
            var date = weekStart.AddDays(offset);
            return new TrainerCalendarDayViewModel
            {
                Date = date,
                DayName = turkishCulture.DateTimeFormat.GetDayName(date.DayOfWeek),
                IsToday = date == DateTime.Today,
                Sessions = sessions.Where(item => item.StartsAtUtc.ToLocalTime().Date == date).ToList()
            };
        }).ToList();

        return View(new TrainerCalendarViewModel
        {
            Sessions = sessions,
            Members = members,
            WeekStart = weekStart,
            WeekEnd = weekEnd,
            Days = days
        });
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
                week = (model.Week ?? DateTime.Today).ToString("yyyy-MM-dd")
            });
        }

        var result = await calendarService.CreateAsync(trainerId.Value, model.MemberProfileId,
            DateTime.SpecifyKind(model.StartsAt, DateTimeKind.Local).ToUniversalTime(),
            model.DurationMinutes, model.Note);
        TempData[result.Succeeded ? "StatusMessage" : "ErrorMessage"] = result.Message;
        return RedirectToAction(nameof(Index), new
        {
            week = (result.Succeeded ? model.StartsAt : model.Week ?? DateTime.Today).ToString("yyyy-MM-dd")
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(UpdateTrainerSessionViewModel model)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var trainerId = await GetTrainerIdAsync();
        if (trainerId is null || string.IsNullOrWhiteSpace(userId)) return Forbid();

        DateTime? postponedUtc = model.PostponedStartsAt is null ? null :
            DateTime.SpecifyKind(model.PostponedStartsAt.Value, DateTimeKind.Local).ToUniversalTime();
        var result = await calendarService.ChangeStatusAsync(
            trainerId.Value, model.Id, model.Status, postponedUtc, userId, model.Note);
        TempData[result.Succeeded ? "StatusMessage" : "ErrorMessage"] = result.Message;
        var targetWeek = model.Status == PersonalTrainingSessionStatus.Postponed &&
            model.PostponedStartsAt is not null
                ? model.PostponedStartsAt.Value
                : model.Week ?? DateTime.Today;
        return RedirectToAction(nameof(Index), new { week = targetWeek.ToString("yyyy-MM-dd") });
    }

    private async Task<int?> GetTrainerIdAsync()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return await dbContext.Trainers.Where(item => item.ApplicationUserId == userId && item.IsActive)
            .Select(item => (int?)item.Id).FirstOrDefaultAsync();
    }
}
