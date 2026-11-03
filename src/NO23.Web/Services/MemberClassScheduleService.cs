using System.Globalization;
using Microsoft.EntityFrameworkCore;
using NO23.Web.Data;
using NO23.Web.Domain.Enums;
using NO23.Web.ViewModels.Member;

namespace NO23.Web.Services;

// Member panel group program: a 14-day window starting today (club time), shown one day or
// one week at a time and optionally narrowed to a lesson type or a single class.
public sealed class MemberClassScheduleService(ApplicationDbContext db)
{
    public const int WindowDays = 14;
    private static readonly CultureInfo TurkishCulture = CultureInfo.GetCultureInfo("tr-TR");

    public async Task<MemberClassScheduleViewModel> GetAsync(int? memberId, MemberClassScheduleQuery query, DateTime nowUtc)
    {
        var today = ClubTime.ToLocal(nowUtc).Date;
        var lastDay = today.AddDays(WindowDays - 1);
        var selected = query.Date?.ToDateTime(TimeOnly.MinValue) ?? today;
        if (selected < today) selected = today;
        if (selected > lastDay) selected = lastDay;

        var isDaily = string.Equals(query.View, MemberClassScheduleViewModel.DayView, StringComparison.OrdinalIgnoreCase);
        var step = isDaily ? 1 : 7;
        // Weeks are counted from today so the window splits into exactly two pages.
        var start = isDaily ? selected : today.AddDays((selected - today).Days / 7 * 7);
        var end = isDaily ? start : start.AddDays(6) < lastDay ? start.AddDays(6) : lastDay;
        var fromUtc = ClubTime.ToUtc(start) > nowUtc ? ClubTime.ToUtc(start) : nowUtc;
        var toUtc = ClubTime.ToUtc(end.AddDays(1));

        var classes = await db.GroupClasses.AsNoTracking()
            .Where(groupClass => groupClass.IsActive)
            .OrderBy(groupClass => groupClass.Name)
            .Select(groupClass => new MemberScheduleClassOption(groupClass.Id, groupClass.Name, groupClass.CreditCategory))
            .ToListAsync();
        LessonCategory? category = query.Category is { } requested && classes.Any(option => option.Category == requested)
            ? requested : null;
        int? classId = query.ClassId is { } id && classes.Any(option => option.Id == id && (category == null || option.Category == category))
            ? id : null;

        var sessions = await db.ClassSessions.AsNoTracking()
            .Where(session =>
                session.Status == ClassSessionStatus.Scheduled &&
                session.GroupClass.IsActive &&
                session.StartsAtUtc >= fromUtc &&
                session.StartsAtUtc < toUtc &&
                (category == null || session.GroupClass.CreditCategory == category) &&
                (classId == null || session.GroupClassId == classId))
            .OrderBy(session => session.StartsAtUtc)
            .ThenBy(session => session.Id)
            .Select(session => new AvailableClassSessionViewModel
            {
                SessionId = session.Id,
                ClassName = session.GroupClass.Name,
                Category = session.GroupClass.CreditCategory,
                TrainerName =
                    session.GroupClass.Trainer.FirstName + " " +
                    session.GroupClass.Trainer.LastName,
                StartsAtUtc = session.StartsAtUtc,
                DurationMinutes = session.GroupClass.DurationMinutes,
                DifficultyLevel = session.GroupClass.DifficultyLevel.ToString(),
                AverageCaloriesBurned = session.GroupClass.AverageCaloriesBurned,
                Capacity = session.CapacityOverride ?? session.GroupClass.Capacity,
                ReservedCount = session.Reservations.Count(reservation =>
                    reservation.Status == ClassReservationStatus.Reserved),
                IsReservedByMember = memberId != null &&
                    session.Reservations.Any(reservation =>
                        reservation.MemberProfileId == memberId &&
                        reservation.Status == ClassReservationStatus.Reserved)
            })
            .ToListAsync();

        var days = Enumerable.Range(0, (end - start).Days + 1)
            .Select(offset => start.AddDays(offset))
            .Select(day => new MemberScheduleDayViewModel
            {
                Date = day,
                DayName = day.ToString("dddd", TurkishCulture),
                Sessions = sessions.Where(session => ClubTime.ToLocal(session.StartsAtUtc).Date == day).ToList()
            })
            .ToList();

        return new MemberClassScheduleViewModel
        {
            View = isDaily ? MemberClassScheduleViewModel.DayView : MemberClassScheduleViewModel.WeekView,
            SelectedDate = selected,
            FirstDay = today,
            LastDay = lastDay,
            RangeStart = start,
            RangeEnd = end,
            PreviousDate = start.AddDays(-step) >= today ? DateOnly.FromDateTime(start.AddDays(-step)) : null,
            NextDate = start.AddDays(step) <= lastDay ? DateOnly.FromDateTime(start.AddDays(step)) : null,
            Category = category,
            ClassId = classId,
            Classes = classes,
            Days = days,
            Sessions = sessions
        };
    }
}
