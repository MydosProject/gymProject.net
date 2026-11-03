using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NO23.Web.Data;
using NO23.Web.Domain.Enums;
using NO23.Web.Services;
using NO23.Web.ViewModels.Classes;

namespace NO23.Web.Controllers;

public class ClassesController(ApplicationDbContext dbContext) : Controller
{
    private const string ReservationTargetUrl = "/Member/Reservations#group-classes";
    private static readonly CultureInfo TurkishCulture = CultureInfo.GetCultureInfo("tr-TR");

    public async Task<IActionResult> Index(DateOnly? week = null)
    {
        var nowUtc = DateTime.UtcNow;
        var currentWeekStart = ClubTime.Monday(ClubTime.Today);
        var weekStart = week is { } requestedWeek
            ? ClubTime.Monday(requestedWeek.ToDateTime(TimeOnly.MinValue))
            : currentWeekStart;
        if (weekStart < currentWeekStart)
        {
            weekStart = currentWeekStart;
        }

        var groupClasses = await dbContext.GroupClasses
            .AsNoTracking()
            .Where(groupClass => groupClass.IsActive)
            .OrderBy(groupClass => groupClass.Name)
            .Select(groupClass => new
            {
                GroupClassId = groupClass.Id,
                groupClass.Name,
                groupClass.Description,
                groupClass.DurationMinutes,
                DifficultyLevel = groupClass.DifficultyLevel.ToString(),
                groupClass.AverageCaloriesBurned,
                TrainerName = groupClass.Trainer.FirstName + " " + groupClass.Trainer.LastName
            })
            .ToListAsync();

        var upcomingSessions = await dbContext.ClassSessions
            .AsNoTracking()
            .Where(session =>
                session.Status == ClassSessionStatus.Scheduled &&
                session.StartsAtUtc >= nowUtc &&
                session.GroupClass.IsActive)
            .OrderBy(session => session.StartsAtUtc)
            .Select(session => new
            {
                ClassSessionId = session.Id,
                session.GroupClassId,
                ClassName = session.GroupClass.Name,
                TrainerName = session.GroupClass.Trainer.FirstName + " " + session.GroupClass.Trainer.LastName,
                session.StartsAtUtc,
                Capacity = session.CapacityOverride ?? session.GroupClass.Capacity,
                ReservedCount = session.Reservations.Count(reservation =>
                    reservation.Status == ClassReservationStatus.Reserved)
            })
            .ToListAsync();

        var sessionViewModels = upcomingSessions
            .Select(session =>
            {
                var startsAtLocal = ClubTime.ToLocal(session.StartsAtUtc);
                return new UpcomingClassSessionPublicViewModel
                {
                    ClassSessionId = session.ClassSessionId,
                    GroupClassId = session.GroupClassId,
                    ClassName = session.ClassName,
                    TrainerName = session.TrainerName,
                    StartsAtUtc = session.StartsAtUtc,
                    StartsAtLocal = startsAtLocal,
                    DayName = startsAtLocal.ToString("dddd", TurkishCulture),
                    Capacity = session.Capacity,
                    ReservedCount = session.ReservedCount,
                    RemainingCapacity = Math.Max(0, session.Capacity - session.ReservedCount)
                };
            })
            .ToList();

        var weekDays = Enumerable.Range(0, 7)
            .Select(offset => weekStart.AddDays(offset))
            .Select(day => new ClassScheduleDayViewModel
            {
                Date = day,
                DayName = day.ToString("dddd", TurkishCulture),
                Sessions = sessionViewModels.Where(session => session.StartsAtLocal.Date == day).ToList()
            })
            .ToList();

        var sessionsByGroupClass = sessionViewModels
            .GroupBy(session => session.GroupClassId)
            .ToDictionary(group => group.Key, group => (IReadOnlyList<UpcomingClassSessionPublicViewModel>)group.ToList());

        var groupClassViewModels = groupClasses
            .Select(groupClass => new GroupClassPublicViewModel
            {
                GroupClassId = groupClass.GroupClassId,
                Name = groupClass.Name,
                Description = groupClass.Description,
                DurationMinutes = groupClass.DurationMinutes,
                DifficultyLevel = groupClass.DifficultyLevel,
                AverageCaloriesBurned = groupClass.AverageCaloriesBurned,
                TrainerName = groupClass.TrainerName,
                UpcomingSessions = sessionsByGroupClass.GetValueOrDefault(groupClass.GroupClassId, [])
            })
            .ToList();

        return View(new ClassesIndexViewModel
        {
            GroupClasses = groupClassViewModels,
            UpcomingSessions = sessionViewModels,
            ReservationTargetUrl = ReservationTargetUrl,
            WeekStart = weekStart,
            PreviousWeek = weekStart > currentWeekStart ? DateOnly.FromDateTime(weekStart.AddDays(-7)) : null,
            NextWeek = DateOnly.FromDateTime(weekStart.AddDays(7)),
            IsCurrentWeek = weekStart == currentWeekStart,
            WeekDays = weekDays
        });
    }
}
