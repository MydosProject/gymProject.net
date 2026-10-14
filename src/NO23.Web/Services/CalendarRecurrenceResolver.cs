using Microsoft.EntityFrameworkCore;
using NO23.Web.Data;
using NO23.Web.Domain.Entities;
using NO23.Web.Domain.Enums;
using NO23.Web.ViewModels.Admin;

namespace NO23.Web.Services;

internal sealed class CalendarRecurrenceResolver(ApplicationDbContext db)
{
    private readonly Dictionary<int, Guid> personalAssignments = [];
    private readonly Dictionary<int, Guid> groupAssignments = [];
    private readonly Dictionary<(int Trainer, int Member), List<PersonalOccurrence>> legacyPersonal = [];
    private readonly Dictionary<int, List<GroupOccurrence>> legacyGroups = [];

    public async Task<int[]> PersonalIdsAsync(PersonalTrainingSession anchor, CalendarBulkScope scope, DateTime now)
    {
        if (anchor.RecurrenceId is { } recurrenceId)
        {
            if (scope == CalendarBulkScope.SelectedLessons) return [anchor.Id];
            return await db.PersonalTrainingSessions.Where(item => item.RecurrenceId == recurrenceId &&
                item.TrainerId == anchor.TrainerId && item.MemberProfileId == anchor.MemberProfileId &&
                item.Status == PersonalTrainingSessionStatus.Scheduled && item.StartsAtUtc > now &&
                (scope != CalendarBulkScope.FollowingOccurrences || item.StartsAtUtc >= anchor.StartsAtUtc))
                .Select(item => item.Id).Take(501).ToArrayAsync();
        }

        var key = (anchor.TrainerId, anchor.MemberProfileId);
        if (!legacyPersonal.TryGetValue(key, out var candidates))
        {
            candidates = await db.PersonalTrainingSessions.AsNoTracking()
                .Where(item => item.RecurrenceId == null && item.TrainerId == anchor.TrainerId && item.MemberProfileId == anchor.MemberProfileId)
                .Select(item => new PersonalOccurrence(item.Id, item.StartsAtUtc, item.Status, item.DurationMinutes, item.Note)).ToListAsync();
            legacyPersonal[key] = candidates;
        }
        var matches = candidates.Where(item => SameWeeklySlot(item.Start, anchor.StartsAtUtc) &&
            item.Duration == anchor.DurationMinutes && string.Equals(item.Note?.Trim() ?? string.Empty,
                anchor.Note?.Trim() ?? string.Empty, StringComparison.Ordinal)).ToList();
        var id = personalAssignments.GetValueOrDefault(anchor.Id, Guid.NewGuid());
        foreach (var match in matches) personalAssignments[match.Id] = id;
        return scope == CalendarBulkScope.SelectedLessons ? [anchor.Id] : matches
            .Where(item => item.Status == PersonalTrainingSessionStatus.Scheduled && item.Start > now &&
                (scope != CalendarBulkScope.FollowingOccurrences || item.Start >= anchor.StartsAtUtc))
            .Select(item => item.Id).Take(501).ToArray();
    }

    public async Task<int[]> GroupIdsAsync(ClassSession anchor, CalendarBulkScope scope, DateTime now)
    {
        if (anchor.RecurrenceId is { } recurrenceId)
        {
            if (scope == CalendarBulkScope.SelectedLessons) return [anchor.Id];
            return await db.ClassSessions.Where(item => item.RecurrenceId == recurrenceId && item.GroupClassId == anchor.GroupClassId &&
                item.Status == ClassSessionStatus.Scheduled && item.StartsAtUtc > now &&
                (scope != CalendarBulkScope.FollowingOccurrences || item.StartsAtUtc >= anchor.StartsAtUtc))
                .Select(item => item.Id).Take(501).ToArrayAsync();
        }

        if (!legacyGroups.TryGetValue(anchor.GroupClassId, out var candidates))
        {
            candidates = await db.ClassSessions.AsNoTracking()
                .Where(item => item.RecurrenceId == null && item.GroupClassId == anchor.GroupClassId)
                .Select(item => new GroupOccurrence(item.Id, item.StartsAtUtc, item.Status, item.CapacityOverride)).ToListAsync();
            legacyGroups[anchor.GroupClassId] = candidates;
        }
        var matches = candidates.Where(item => SameWeeklySlot(item.Start, anchor.StartsAtUtc) && item.Capacity == anchor.CapacityOverride).ToList();
        var id = groupAssignments.GetValueOrDefault(anchor.Id, Guid.NewGuid());
        foreach (var match in matches) groupAssignments[match.Id] = id;
        return scope == CalendarBulkScope.SelectedLessons ? [anchor.Id] : matches
            .Where(item => item.Status == ClassSessionStatus.Scheduled && item.Start > now &&
                (scope != CalendarBulkScope.FollowingOccurrences || item.Start >= anchor.StartsAtUtc))
            .Select(item => item.Id).Take(501).ToArray();
    }

    // Save legacy identities only after every schedule validation has succeeded.
    // Earlier occurrences stay linked when only the following occurrences move.
    public async Task AssignLegacyIdentitiesAsync()
    {
        var personalIds = personalAssignments.Keys.ToArray();
        var groupIds = groupAssignments.Keys.ToArray();
        if (personalIds.Length > 0)
            foreach (var item in await db.PersonalTrainingSessions.Where(item => personalIds.Contains(item.Id)).ToListAsync())
                item.RecurrenceId = personalAssignments[item.Id];
        if (groupIds.Length > 0)
            foreach (var item in await db.ClassSessions.Where(item => groupIds.Contains(item.Id)).ToListAsync())
                item.RecurrenceId = groupAssignments[item.Id];
    }

    private static bool SameWeeklySlot(DateTime first, DateTime second)
    {
        var firstLocal = ClubTime.ToLocal(first);
        var secondLocal = ClubTime.ToLocal(second);
        return firstLocal.DayOfWeek == secondLocal.DayOfWeek && firstLocal.TimeOfDay == secondLocal.TimeOfDay;
    }

    private sealed record PersonalOccurrence(int Id, DateTime Start, PersonalTrainingSessionStatus Status, int Duration, string? Note);
    private sealed record GroupOccurrence(int Id, DateTime Start, ClassSessionStatus Status, int? Capacity);
}
