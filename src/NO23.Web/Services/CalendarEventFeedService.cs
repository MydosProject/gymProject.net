using System.Globalization;
using Microsoft.EntityFrameworkCore;
using NO23.Web.Data;
using NO23.Web.Domain.Enums;
using NO23.Web.Extensions;
using NO23.Web.ViewModels;

namespace NO23.Web.Services;

// JSON feed for the interactive admin / trainer calendar. Filters mirror the calendar pages:
// cancelled lessons are hidden, completed ones only on request, consultations not in a member's view.
public sealed class CalendarEventFeedService(ApplicationDbContext db)
{
    public const int MaxRangeDays = 62;
    private const string LocalFormat = "yyyy-MM-ddTHH:mm:ss";

    public static bool TryParseRange(string? start, string? end, out DateTime fromUtc, out DateTime toUtc)
    {
        toUtc = default;
        return TryParseUtc(start, out fromUtc) && TryParseUtc(end, out toUtc) &&
            toUtc > fromUtc && (toUtc - fromUtc).TotalDays <= MaxRangeDays;
    }

    public async Task<IReadOnlyList<CalendarFeedEvent>> GetAsync(CalendarFeedQuery query, DateTime nowUtc)
    {
        var personal = await db.PersonalTrainingSessions.AsNoTracking()
            .Where(x => x.Status != PersonalTrainingSessionStatus.Cancelled &&
                (query.IncludeCompleted || x.Status != PersonalTrainingSessionStatus.Completed) &&
                x.StartsAtUtc >= query.FromUtc && x.StartsAtUtc < query.ToUtc &&
                (!query.TrainerId.HasValue || x.TrainerId == query.TrainerId) &&
                (!query.MemberId.HasValue || x.MemberProfileId == query.MemberId))
            .Select(x => new
            {
                x.Id, x.MemberProfileId, x.TrainerId, x.StartsAtUtc, x.DurationMinutes, x.Status,
                TrainerName = (x.Trainer.FirstName ?? "") + " " + (x.Trainer.LastName ?? ""),
                MemberName = (x.MemberProfile.ApplicationUser.FirstName ?? "") + " " + (x.MemberProfile.ApplicationUser.LastName ?? "")
            })
            .ToListAsync();
        var groups = await db.ClassSessions.AsNoTracking()
            .Where(x => x.Status != ClassSessionStatus.Cancelled &&
                (query.IncludeCompleted || x.Status != ClassSessionStatus.Completed) && x.GroupClass.IsActive &&
                x.StartsAtUtc >= query.FromUtc && x.StartsAtUtc < query.ToUtc &&
                (!query.TrainerId.HasValue || x.GroupClass.TrainerId == query.TrainerId) &&
                (!query.MemberId.HasValue || x.Reservations.Any(r => r.MemberProfileId == query.MemberId && r.Status != ClassReservationStatus.Cancelled)))
            .Select(x => new
            {
                x.Id, x.StartsAtUtc, x.Status, x.GroupClass.Name, x.GroupClass.DurationMinutes, x.GroupClass.TrainerId,
                TrainerName = (x.GroupClass.Trainer.FirstName ?? "") + " " + (x.GroupClass.Trainer.LastName ?? ""),
                Capacity = x.CapacityOverride ?? x.GroupClass.Capacity,
                Participants = x.Reservations.Where(r => r.Status == ClassReservationStatus.Reserved)
                    .Select(r => (r.MemberProfile.ApplicationUser.FirstName ?? "") + " " + (r.MemberProfile.ApplicationUser.LastName ?? "")).ToList()
            })
            .ToListAsync();
        var appointments = query.MemberId.HasValue ? [] : await db.AppointmentRequests.AsNoTracking()
            .Where(x => x.ScheduledAtUtc >= query.FromUtc && x.ScheduledAtUtc < query.ToUtc &&
                (x.ScheduleStatus == AppointmentScheduleStatus.Scheduled ||
                    (query.IncludeCompleted && x.ScheduleStatus == AppointmentScheduleStatus.Completed)) &&
                (!query.TrainerId.HasValue || x.ScheduledTrainerId == query.TrainerId))
            .Select(x => new
            {
                x.Id, x.FullName, x.ScheduledAtUtc, x.DurationMinutes, x.ScheduleStatus, x.ScheduledTrainerId,
                TrainerName = x.ScheduledTrainer == null ? null : (x.ScheduledTrainer.FirstName ?? "") + " " + (x.ScheduledTrainer.LastName ?? "")
            })
            .ToListAsync();
        var progress = await LoadProgressAsync(personal.Select(x => (x.Id, x.MemberProfileId)));

        return personal.Select(x => new CalendarFeedEvent
            {
                Id = $"{CalendarMoveInput.Personal}-{x.Id}", Kind = CalendarMoveInput.Personal, EntityId = x.Id,
                Title = x.MemberName.Trim(), Start = Local(x.StartsAtUtc), End = Local(x.StartsAtUtc.AddMinutes(x.DurationMinutes)),
                StartEditable = x.Status == PersonalTrainingSessionStatus.Scheduled && x.StartsAtUtc > nowUtc,
                DurationEditable = x.Status == PersonalTrainingSessionStatus.Scheduled && x.StartsAtUtc > nowUtc,
                TrainerId = x.TrainerId, TrainerName = x.TrainerName.Trim(), StatusName = x.Status.GetDisplayName(),
                IsDone = x.Status is PersonalTrainingSessionStatus.Completed or PersonalTrainingSessionStatus.NoShow,
                Progress = progress.GetValueOrDefault(x.Id)?.Describe()
            })
            .Concat(groups.Select(x => new CalendarFeedEvent
            {
                Id = $"{CalendarMoveInput.Group}-{x.Id}", Kind = CalendarMoveInput.Group, EntityId = x.Id,
                Title = x.Name, Start = Local(x.StartsAtUtc), End = Local(x.StartsAtUtc.AddMinutes(x.DurationMinutes)),
                StartEditable = x.Status == ClassSessionStatus.Scheduled && x.StartsAtUtc > nowUtc,
                TrainerId = x.TrainerId, TrainerName = x.TrainerName.Trim(), StatusName = x.Status.GetDisplayName(),
                IsDone = x.Status == ClassSessionStatus.Completed,
                Capacity = x.Capacity, ReservedCount = x.Participants.Count,
                Participants = x.Participants.Select(name => name.Trim()).ToList()
            }))
            .Concat(appointments.Select(x => new CalendarFeedEvent
            {
                Id = $"consultation-{x.Id}", Kind = "consultation", EntityId = x.Id,
                Title = x.FullName, Start = Local(x.ScheduledAtUtc!.Value), End = Local(x.ScheduledAtUtc.Value.AddMinutes(x.DurationMinutes)),
                TrainerId = x.ScheduledTrainerId, TrainerName = x.TrainerName?.Trim(),
                StatusName = x.ScheduleStatus?.GetDisplayName() ?? string.Empty,
                IsDone = x.ScheduleStatus == AppointmentScheduleStatus.Completed
            }))
            .OrderBy(x => x.Start, StringComparer.Ordinal)
            .ToList();
    }

    // "Which lesson of the package is this": PT sessions of the member's current period, counted in time order.
    public async Task<IReadOnlyDictionary<int, CalendarLessonProgress>> LoadProgressAsync(IEnumerable<(int SessionId, int MemberId)> sessions)
    {
        var items = sessions.Distinct().ToList();
        var memberIds = items.Select(x => x.MemberId).Distinct().ToArray();
        if (memberIds.Length == 0) return new Dictionary<int, CalendarLessonProgress>();
        var members = await db.MemberProfiles.AsNoTracking().Where(x => memberIds.Contains(x.Id))
            .Select(x => new
            {
                x.Id, x.MembershipEndsAtUtc,
                PackageName = x.ServicePackageVariant == null ? x.MembershipPackage.Name
                    : x.ServicePackageVariant.ServicePackage.Name + " — " + x.ServicePackageVariant.Name
            })
            .ToDictionaryAsync(x => x.Id);
        var periodSessions = await db.PersonalTrainingSessions.AsNoTracking()
            .Where(x => memberIds.Contains(x.MemberProfileId) && x.Status != PersonalTrainingSessionStatus.Cancelled &&
                (x.LessonAccount == null || x.LessonAccount.PeriodId == x.MemberProfile.LessonPeriodId) &&
                (!x.MemberProfile.MembershipStartsAtUtc.HasValue || x.StartsAtUtc >= x.MemberProfile.MembershipStartsAtUtc.Value) &&
                (!x.MemberProfile.MembershipEndsAtUtc.HasValue || x.StartsAtUtc < x.MemberProfile.MembershipEndsAtUtc.Value))
            .Select(x => new { x.Id, x.MemberProfileId, x.StartsAtUtc })
            .ToListAsync();
        var ordinals = periodSessions.GroupBy(x => x.MemberProfileId)
            .SelectMany(member => member.OrderBy(x => x.StartsAtUtc).ThenBy(x => x.Id).Select((x, index) => (x.Id, Ordinal: index + 1)))
            .ToDictionary(x => x.Id, x => x.Ordinal);
        var summaries = await new LessonCreditSummaryService(db).LoadAsync(memberIds);

        var result = new Dictionary<int, CalendarLessonProgress>();
        foreach (var (sessionId, memberId) in items)
        {
            if (!members.TryGetValue(memberId, out var member)) continue;
            var summary = summaries.GetValueOrDefault(memberId);
            var account = summary?.Categories.FirstOrDefault(x => x.Category == LessonCategory.PersonalTraining);
            var limited = summary is { IsUnlimited: false };
            result[sessionId] = new CalendarLessonProgress(member.PackageName,
                ordinals.TryGetValue(sessionId, out var ordinal) ? ordinal : null,
                !limited ? null : account is not null ? account.Granted + account.Converted : summary!.PersonalTrainingTotal,
                limited ? account?.Remaining : null,
                member.MembershipEndsAtUtc);
        }
        return result;
    }

    private static string Local(DateTime utc) => ClubTime.ToLocal(utc).ToString(LocalFormat, CultureInfo.InvariantCulture);

    private static bool TryParseUtc(string? value, out DateTime utc)
    {
        utc = default;
        if (!DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var parsed)) return false;
        // FullCalendar sends the browser offset; a value without one is club time.
        utc = parsed.Kind == DateTimeKind.Unspecified ? ClubTime.ToUtc(parsed) : parsed.ToUniversalTime();
        return utc.Year is >= 1900 and <= 2100;
    }
}
