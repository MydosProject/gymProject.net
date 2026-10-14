using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NO23.Web.Data;
using NO23.Web.Domain.Entities;
using NO23.Web.Domain.Enums;
using NO23.Web.Services;
using NO23.Web.ViewModels.Admin;
using NO23.Web.ViewModels.TrainerPanel;

namespace NO23.Tests;

public class CalendarRecurrenceOperationTests
{
    [Fact]
    public async Task WeeklyPersonalCreator_AssignsDistinctSeriesPerWeekdayAndPerCreationBatch()
    {
        await using var db = Db();
        var (trainer, member, monday) = await Seed(db);
        member.MembershipEndsAtUtc = null;
        await db.SaveChangesAsync();
        var service = new PersonalTrainingCalendarService(db);
        var input = new WeeklyPersonalSessionInput { MemberProfileId = member.Id, Week = monday, Weeks = 2,
            Days = [DayOfWeek.Monday, DayOfWeek.Wednesday, DayOfWeek.Monday], Time = new(11, 0), DurationMinutes = 50 };

        var firstResult = await service.CreateWeeklyAsync(trainer.Id, input);

        Assert.True(firstResult.Succeeded, firstResult.Message);
        var firstBatch = await db.PersonalTrainingSessions.OrderBy(item => item.StartsAtUtc).ToListAsync();
        Assert.Equal(4, firstBatch.Count);
        Assert.Equal(2, firstBatch.Select(item => item.RecurrenceId).Distinct().Count());
        Assert.All(firstBatch, item => Assert.True(item.RecurrenceId.HasValue && item.RecurrenceId != Guid.Empty));
        foreach (var day in firstBatch.GroupBy(item => ClubTime.ToLocal(item.StartsAtUtc).DayOfWeek))
            Assert.Single(day.Select(item => item.RecurrenceId).Distinct());

        input.Week = monday.AddDays(28);
        var secondResult = await service.CreateWeeklyAsync(trainer.Id, input);

        Assert.True(secondResult.Succeeded, secondResult.Message);
        Assert.Equal(8, await db.PersonalTrainingSessions.CountAsync());
        Assert.Equal(4, (await db.PersonalTrainingSessions.Select(item => item.RecurrenceId).ToListAsync()).Distinct().Count());
    }

    [Fact]
    public async Task WeeklyGroupCreator_AssignsDistinctSeriesPerWeekdayAndPerCreationBatch()
    {
        await using var db = Db();
        var (trainer, _, monday) = await Seed(db);
        var group = Group(trainer);
        db.GroupClasses.Add(group);
        await db.SaveChangesAsync();
        var service = new WeeklyGroupSchedulingService(db);
        var input = new WeeklyGroupInput { GroupClassId = group.Id, Week = monday, Weeks = 2, Capacity = 8,
            Days = [DayOfWeek.Monday, DayOfWeek.Wednesday, DayOfWeek.Wednesday], Time = new(11, 0) };

        var firstResult = await service.CreateAsync(input);

        Assert.True(firstResult.Succeeded, firstResult.Message);
        var firstBatch = await db.ClassSessions.OrderBy(item => item.StartsAtUtc).ToListAsync();
        Assert.Equal(4, firstBatch.Count);
        Assert.Equal(2, firstBatch.Select(item => item.RecurrenceId).Distinct().Count());
        Assert.All(firstBatch, item => Assert.True(item.RecurrenceId.HasValue && item.RecurrenceId != Guid.Empty));
        foreach (var day in firstBatch.GroupBy(item => ClubTime.ToLocal(item.StartsAtUtc).DayOfWeek))
            Assert.Single(day.Select(item => item.RecurrenceId).Distinct());

        input.Week = monday.AddDays(28);
        var secondResult = await service.CreateAsync(input);

        Assert.True(secondResult.Succeeded, secondResult.Message);
        Assert.Equal(8, await db.ClassSessions.CountAsync());
        Assert.Equal(4, (await db.ClassSessions.Select(item => item.RecurrenceId).ToListAsync()).Distinct().Count());
    }

    [Fact]
    public async Task StandalonePersonalCreator_AssignsSeparateIdentitiesToSameSlotOnDifferentWeeks()
    {
        await using var db = Db();
        var (trainer, member, monday) = await Seed(db);
        var service = new PersonalTrainingCalendarService(db);

        var firstResult = await service.CreateAsync(trainer.Id, member.Id, ClubTime.ToUtc(monday.AddHours(11)), 50, null);
        var secondResult = await service.CreateAsync(trainer.Id, member.Id, ClubTime.ToUtc(monday.AddDays(7).AddHours(11)), 50, null);

        Assert.True(firstResult.Succeeded, firstResult.Message);
        Assert.True(secondResult.Succeeded, secondResult.Message);
        var sessions = await db.PersonalTrainingSessions.ToListAsync();
        Assert.Equal(2, sessions.Count);
        Assert.All(sessions, item => Assert.True(item.RecurrenceId.HasValue && item.RecurrenceId != Guid.Empty));
        Assert.Equal(2, sessions.Select(item => item.RecurrenceId).Distinct().Count());
    }

    [Fact]
    public async Task DeleteAllUpcomingPersonal_LeavesPastResultsAndOtherSeriesUntouched()
    {
        await using var db = Db();
        var (trainer, member, monday) = await Seed(db);
        var recurrence = Guid.NewGuid();
        var first = Personal(trainer, member, monday.AddHours(11), recurrence);
        var second = Personal(trainer, member, monday.AddDays(7).AddHours(11), recurrence);
        first.CreditConsumed = true;
        second.CreditConsumed = true;
        var past = Personal(trainer, member, ClubTime.Monday(ClubTime.Now.AddDays(-14)).AddHours(11), recurrence);
        var completed = Personal(trainer, member, monday.AddDays(14).AddHours(11), recurrence);
        completed.Status = PersonalTrainingSessionStatus.Completed;
        var cancelled = Personal(trainer, member, monday.AddDays(21).AddHours(11), recurrence);
        cancelled.Status = PersonalTrainingSessionStatus.Cancelled;
        var otherDay = Personal(trainer, member, monday.AddDays(2).AddHours(11), Guid.NewGuid());
        var otherBatch = Personal(trainer, member, monday.AddDays(28).AddHours(11), Guid.NewGuid());
        var otherMember = Personal(trainer, Member(trainer, member.MembershipPackage), monday.AddDays(35).AddHours(11), Guid.NewGuid());
        db.PersonalTrainingSessions.AddRange(first, second, past, completed, cancelled, otherDay, otherBatch, otherMember);
        await db.SaveChangesAsync();
        var credits = member.RemainingClassCredits;

        var result = await Apply(db, new() { PersonalIds = [second.Id], MemberId = member.Id,
            Scope = CalendarBulkScope.AllUpcomingOccurrences, Operation = CalendarBulkOperation.Delete });

        Assert.True(result.Succeeded, result.Message);
        Assert.Equal(new[] { past.Id, completed.Id, cancelled.Id, otherDay.Id, otherBatch.Id, otherMember.Id }.Order(),
            (await db.PersonalTrainingSessions.Select(item => item.Id).ToListAsync()).Order());
        Assert.Equal(credits + 2, member.RemainingClassCredits);
    }

    [Theory]
    [InlineData(CalendarBulkScope.FollowingOccurrences, 1)]
    [InlineData(CalendarBulkScope.AllUpcomingOccurrences, 0)]
    public async Task Scope_UsesAnchorForFollowingAndIncludesEarlierFutureForAll(CalendarBulkScope scope, int remaining)
    {
        await using var db = Db();
        var (trainer, member, monday) = await Seed(db);
        var recurrence = Guid.NewGuid();
        var sessions = Enumerable.Range(0, 3).Select(index => Personal(trainer, member,
            monday.AddDays(index * 7).AddHours(11), recurrence)).ToArray();
        db.PersonalTrainingSessions.AddRange(sessions);
        await db.SaveChangesAsync();

        var result = await Apply(db, new() { PersonalIds = [sessions[1].Id, sessions[2].Id],
            Scope = scope, Operation = CalendarBulkOperation.Delete });

        Assert.True(result.Succeeded, result.Message);
        Assert.Equal(remaining, await db.PersonalTrainingSessions.CountAsync());
        if (remaining == 1) Assert.Equal(sessions[0].Id, (await db.PersonalTrainingSessions.SingleAsync()).Id);
    }

    [Fact]
    public async Task UpdateDayOnly_MovesEachOccurrenceWithinItsLocalWeekAndPreservesTimeIdentityAndCredits()
    {
        await using var db = Db();
        var (trainer, member, monday) = await Seed(db);
        var recurrence = Guid.NewGuid();
        var sessions = Enumerable.Range(0, 3).Select(index => Personal(trainer, member,
            monday.AddDays(index * 7 + 6).AddHours(23).AddMinutes(30), recurrence)).ToArray();
        db.PersonalTrainingSessions.AddRange(sessions);
        await db.SaveChangesAsync();
        var credits = member.RemainingClassCredits;
        var before = sessions.Select(item => item.StartsAtUtc).ToArray();

        var result = await Apply(db, new() { PersonalIds = [sessions[1].Id],
            Scope = CalendarBulkScope.AllUpcomingOccurrences, Operation = CalendarBulkOperation.UpdateTime,
            Day = DayOfWeek.Monday });

        Assert.True(result.Succeeded, result.Message);
        for (var index = 0; index < sessions.Length; index++)
        {
            Assert.Equal(ClubTime.ToUtc(monday.AddDays(index * 7).AddHours(23).AddMinutes(30)), sessions[index].StartsAtUtc);
            Assert.Equal(recurrence, sessions[index].RecurrenceId);
            Assert.Equal(PersonalTrainingSessionStatus.Scheduled, sessions[index].Status);
            var history = Assert.Single(sessions[index].History);
            Assert.Equal(before[index], history.PreviousStartsAtUtc);
            Assert.Equal(sessions[index].StartsAtUtc, history.NewStartsAtUtc);
        }
        Assert.Equal(credits, member.RemainingClassCredits);
    }

    [Fact]
    public async Task UpdateTimeOnly_KeepsLocalDayOfEachRecurringOccurrence()
    {
        await using var db = Db();
        var (trainer, member, monday) = await Seed(db);
        var recurrence = Guid.NewGuid();
        var first = Personal(trainer, member, monday.AddHours(11), recurrence);
        var rescheduled = Personal(trainer, member, monday.AddDays(9).AddHours(14), recurrence);
        db.PersonalTrainingSessions.AddRange(first, rescheduled);
        await db.SaveChangesAsync();

        var result = await Apply(db, new() { PersonalIds = [rescheduled.Id], Scope = CalendarBulkScope.AllUpcomingOccurrences,
            Operation = CalendarBulkOperation.UpdateTime, Time = new(17, 15) });

        Assert.True(result.Succeeded, result.Message);
        Assert.Equal(ClubTime.ToUtc(monday.AddHours(17).AddMinutes(15)), first.StartsAtUtc);
        Assert.Equal(ClubTime.ToUtc(monday.AddDays(9).AddHours(17).AddMinutes(15)), rescheduled.StartsAtUtc);
    }

    [Fact]
    public async Task SingleOccurrenceMove_PreservesSeriesForSubsequentWholeSeriesUpdate()
    {
        await using var db = Db();
        var (trainer, member, monday) = await Seed(db);
        var recurrence = Guid.NewGuid();
        var first = Personal(trainer, member, monday.AddHours(11), recurrence);
        var second = Personal(trainer, member, monday.AddDays(7).AddHours(11), recurrence);
        db.PersonalTrainingSessions.AddRange(first, second);
        await db.SaveChangesAsync();
        var moved = await Apply(db, new() { PersonalIds = [first.Id], Operation = CalendarBulkOperation.UpdateTime,
            Day = DayOfWeek.Wednesday, Time = new(13, 0) });
        Assert.True(moved.Succeeded, moved.Message);
        Assert.Equal(recurrence, first.RecurrenceId);
        Assert.Equal(ClubTime.ToUtc(monday.AddDays(7).AddHours(11)), second.StartsAtUtc);

        var result = await Apply(db, new() { PersonalIds = [first.Id], Scope = CalendarBulkScope.AllUpcomingOccurrences,
            Operation = CalendarBulkOperation.UpdateTime, Day = DayOfWeek.Friday, Time = new(15, 0) });

        Assert.True(result.Succeeded, result.Message);
        Assert.Equal(ClubTime.ToUtc(monday.AddDays(4).AddHours(15)), first.StartsAtUtc);
        Assert.Equal(ClubTime.ToUtc(monday.AddDays(11).AddHours(15)), second.StartsAtUtc);
        Assert.Equal(recurrence, second.RecurrenceId);
        Assert.Equal(3, await db.PersonalTrainingSessionHistories.CountAsync());
    }

    [Fact]
    public async Task LegacySinglePersonalMove_LinksUntouchedOccurrencesBeforeChangingAnchorSlot()
    {
        await using var db = Db();
        var (trainer, member, monday) = await Seed(db);
        var first = Personal(trainer, member, monday.AddHours(11));
        var second = Personal(trainer, member, monday.AddDays(7).AddHours(11));
        var past = Personal(trainer, member, ClubTime.Monday(ClubTime.Now.AddDays(-14)).AddHours(11));
        var otherDay = Personal(trainer, member, monday.AddDays(3).AddHours(11));
        db.PersonalTrainingSessions.AddRange(first, second, past, otherDay);
        await db.SaveChangesAsync();
        var secondStart = second.StartsAtUtc;

        var moved = await Apply(db, new() { PersonalIds = [first.Id], Operation = CalendarBulkOperation.UpdateTime,
            Day = DayOfWeek.Wednesday, Time = new(13, 0) });

        Assert.True(moved.Succeeded, moved.Message);
        Assert.NotNull(first.RecurrenceId);
        Assert.Equal(first.RecurrenceId, second.RecurrenceId);
        Assert.Equal(first.RecurrenceId, past.RecurrenceId);
        Assert.Null(otherDay.RecurrenceId);
        Assert.Equal(secondStart, second.StartsAtUtc);
        var deleted = await Apply(db, new() { PersonalIds = [first.Id], Scope = CalendarBulkScope.AllUpcomingOccurrences,
            Operation = CalendarBulkOperation.Delete });

        Assert.True(deleted.Succeeded, deleted.Message);
        Assert.Equal(new[] { past.Id, otherDay.Id }.Order(), (await db.PersonalTrainingSessions.Select(item => item.Id).ToListAsync()).Order());
    }

    [Fact]
    public async Task LegacySingleGroupMove_LinksUntouchedOccurrencesBeforeChangingAnchorSlot()
    {
        await using var db = Db();
        var (trainer, member, monday) = await Seed(db);
        var group = Group(trainer);
        var first = Session(group, monday.AddHours(11), null, member);
        var second = Session(group, monday.AddDays(7).AddHours(11), null, member);
        var past = Session(group, ClubTime.Monday(ClubTime.Now.AddDays(-14)).AddHours(11), null, member);
        var otherDay = Session(group, monday.AddDays(3).AddHours(11), null, member);
        db.ClassSessions.AddRange(first, second, past, otherDay);
        await db.SaveChangesAsync();
        var secondStart = second.StartsAtUtc;

        var moved = await Apply(db, new() { GroupIds = [first.Id], Operation = CalendarBulkOperation.UpdateTime,
            Day = DayOfWeek.Wednesday, Time = new(13, 0) });

        Assert.True(moved.Succeeded, moved.Message);
        Assert.NotNull(first.RecurrenceId);
        Assert.Equal(first.RecurrenceId, second.RecurrenceId);
        Assert.Equal(first.RecurrenceId, past.RecurrenceId);
        Assert.Null(otherDay.RecurrenceId);
        Assert.Equal(secondStart, second.StartsAtUtc);
        var deleted = await Apply(db, new() { GroupIds = [first.Id], Scope = CalendarBulkScope.AllUpcomingOccurrences,
            Operation = CalendarBulkOperation.Delete });

        Assert.True(deleted.Succeeded, deleted.Message);
        Assert.Equal(new[] { past.Id, otherDay.Id }.Order(), (await db.ClassSessions.Select(item => item.Id).ToListAsync()).Order());
    }

    [Fact]
    public async Task LegacyIndividualPersonalPostpone_PreservesSeriesForLaterBulkUpdateFromMovedAnchor()
    {
        await using var db = Db();
        var (trainer, member, monday) = await Seed(db);
        var first = Personal(trainer, member, monday.AddHours(11));
        var second = Personal(trainer, member, monday.AddDays(7).AddHours(11));
        second.Note = "  ";
        var otherDay = Personal(trainer, member, monday.AddDays(3).AddHours(11));
        db.PersonalTrainingSessions.AddRange(first, second, otherDay);
        await db.SaveChangesAsync();
        var secondStart = second.StartsAtUtc;
        var otherStart = otherDay.StartsAtUtc;
        var credits = member.RemainingClassCredits;

        var postponed = await new PersonalTrainingCalendarService(db).ChangeStatusAsync(trainer.Id, first.Id,
            PersonalTrainingSessionStatus.Postponed, ClubTime.ToUtc(monday.AddDays(2).AddHours(13)), "trainer", "Individual postpone");

        Assert.True(postponed.Succeeded, postponed.Message);
        Assert.NotNull(first.RecurrenceId);
        Assert.Equal(first.RecurrenceId, second.RecurrenceId);
        Assert.Equal(secondStart, second.StartsAtUtc);
        Assert.Null(otherDay.RecurrenceId);
        var updated = await Apply(db, new() { PersonalIds = [first.Id], Scope = CalendarBulkScope.AllUpcomingOccurrences,
            Operation = CalendarBulkOperation.UpdateTime, Day = DayOfWeek.Friday, Time = new(15, 0) });

        Assert.True(updated.Succeeded, updated.Message);
        Assert.Equal(ClubTime.ToUtc(monday.AddDays(4).AddHours(15)), first.StartsAtUtc);
        Assert.Equal(ClubTime.ToUtc(monday.AddDays(11).AddHours(15)), second.StartsAtUtc);
        Assert.Equal(otherStart, otherDay.StartsAtUtc);
        Assert.Equal(credits, member.RemainingClassCredits);
        Assert.Equal(3, await db.PersonalTrainingSessionHistories.CountAsync());
    }

    [Fact]
    public async Task LegacyIndividualGroupCapacityEdit_PreservesSeriesForLaterBulkDelete()
    {
        await using var db = Db();
        var (trainer, member, monday) = await Seed(db);
        var group = Group(trainer);
        var first = Session(group, monday.AddHours(11), null, member);
        var second = Session(group, monday.AddDays(7).AddHours(11), null, member);
        var otherCapacity = Session(group, monday.AddDays(14).AddHours(11), null, member);
        first.CapacityOverride = second.CapacityOverride = 6;
        otherCapacity.CapacityOverride = 7;
        db.ClassSessions.AddRange(first, second, otherCapacity);
        await db.SaveChangesAsync();
        var start = first.StartsAtUtc;
        var controller = new NO23.Web.Areas.Admin.Controllers.ClassSessionsController(db, new(db), null!);

        var edited = await controller.Edit(first.Id, new ClassSessionFormViewModel { Id = first.Id,
            GroupClassId = group.Id, StartsAtLocal = ClubTime.ToLocal(start), CapacityOverride = 8, Status = ClassSessionStatus.Scheduled });

        Assert.IsType<RedirectToActionResult>(edited);
        Assert.Equal(start, first.StartsAtUtc);
        Assert.Equal(8, first.CapacityOverride);
        Assert.Equal(6, second.CapacityOverride);
        Assert.NotNull(first.RecurrenceId);
        Assert.Equal(first.RecurrenceId, second.RecurrenceId);
        Assert.Null(otherCapacity.RecurrenceId);
        var deleted = await Apply(db, new() { GroupIds = [first.Id], Scope = CalendarBulkScope.AllUpcomingOccurrences,
            Operation = CalendarBulkOperation.Delete });

        Assert.True(deleted.Succeeded, deleted.Message);
        Assert.Equal(otherCapacity.Id, (await db.ClassSessions.SingleAsync()).Id);
        Assert.Equal(1002, member.RemainingClassCredits);
        Assert.Single(db.ClassReservations);
    }

    [Fact]
    public async Task LegacyPersonal_ExpandsOnlyMatchingNullIdentityWeekdayTimeDurationNoteMemberAndTrainer()
    {
        await using var db = Db();
        var (trainer, member, monday) = await Seed(db);
        var first = Personal(trainer, member, monday.AddHours(11));
        var second = Personal(trainer, member, monday.AddDays(7).AddHours(11));
        first.Note = second.Note = "Legacy weekly lesson";
        var differentDay = Personal(trainer, member, monday.AddDays(2).AddHours(11));
        var differentTime = Personal(trainer, member, monday.AddDays(14).AddHours(12));
        var differentDuration = Personal(trainer, member, monday.AddDays(21).AddHours(11));
        differentDuration.DurationMinutes = 60;
        var differentNote = Personal(trainer, member, monday.AddDays(28).AddHours(11));
        differentNote.Note = "Other weekly lesson";
        var differentMember = Personal(trainer, Member(trainer, member.MembershipPackage), monday.AddDays(35).AddHours(11));
        var differentTrainer = Personal(Trainer(), member, monday.AddDays(42).AddHours(11));
        var identified = Personal(trainer, member, monday.AddDays(49).AddHours(11), Guid.NewGuid());
        foreach (var item in new[] { differentDay, differentTime, differentDuration, differentMember, differentTrainer, identified })
            item.Note = first.Note;
        var untouched = new[] { differentDay, differentTime, differentDuration, differentNote, differentMember, differentTrainer, identified };
        db.PersonalTrainingSessions.AddRange(untouched.Append(first).Append(second));
        await db.SaveChangesAsync();

        var result = await Apply(db, new() { PersonalIds = [first.Id], Scope = CalendarBulkScope.AllUpcomingOccurrences,
            Operation = CalendarBulkOperation.Delete });

        Assert.True(result.Succeeded, result.Message);
        Assert.Equal(untouched.Select(item => item.Id).Order(),
            (await db.PersonalTrainingSessions.Select(item => item.Id).ToListAsync()).Order());
    }

    [Fact]
    public async Task DeleteGroupSeries_ExpandsBeyondSelectedMemberAndRefundsEveryReservedParticipantOnce()
    {
        await using var db = Db();
        var (trainer, member, monday) = await Seed(db);
        var other = Member(trainer, member.MembershipPackage);
        var recurrence = Guid.NewGuid();
        var group = Group(trainer);
        var first = Session(group, monday.AddHours(11), recurrence, member);
        var second = Session(group, monday.AddDays(7).AddHours(11), recurrence, other);
        second.Reservations.Add(new() { MemberProfile = member, Status = ClassReservationStatus.Cancelled });
        var third = Session(group, monday.AddDays(14).AddHours(11), recurrence, member, other);
        var past = Session(group, ClubTime.Monday(ClubTime.Now.AddDays(-14)).AddHours(11), recurrence, member);
        var completed = Session(group, monday.AddDays(21).AddHours(11), recurrence, member);
        completed.Status = ClassSessionStatus.Completed;
        var otherSeries = Session(group, monday.AddDays(2).AddHours(11), Guid.NewGuid(), member);
        db.ClassSessions.AddRange(first, second, third, past, completed, otherSeries);
        await db.SaveChangesAsync();
        var memberCredits = member.RemainingClassCredits;
        var otherCredits = other.RemainingClassCredits;
        var input = new CalendarBulkInput { GroupIds = [first.Id, third.Id], MemberId = member.Id,
            Scope = CalendarBulkScope.AllUpcomingOccurrences, Operation = CalendarBulkOperation.Delete };

        var result = await Apply(db, input);

        Assert.True(result.Succeeded, result.Message);
        Assert.Equal(new[] { past.Id, completed.Id, otherSeries.Id }.Order(),
            (await db.ClassSessions.Select(item => item.Id).ToListAsync()).Order());
        Assert.Equal(memberCredits + 2, member.RemainingClassCredits);
        Assert.Equal(otherCredits + 2, other.RemainingClassCredits);
        Assert.Equal(3, await db.ClassReservations.CountAsync());
        Assert.False((await Apply(db, input)).Succeeded);
        Assert.Equal(memberCredits + 2, member.RemainingClassCredits);
        Assert.Equal(otherCredits + 2, other.RemainingClassCredits);
    }

    [Theory]
    [InlineData(CalendarBulkScope.FollowingOccurrences, 1)]
    [InlineData(CalendarBulkScope.AllUpcomingOccurrences, 0)]
    public async Task GroupScope_UsesAnchorForFollowingAndIncludesEarlierFutureForAll(CalendarBulkScope scope, int remaining)
    {
        await using var db = Db();
        var (trainer, member, monday) = await Seed(db);
        var recurrence = Guid.NewGuid();
        var group = Group(trainer);
        var sessions = Enumerable.Range(0, 3).Select(index => Session(group,
            monday.AddDays(index * 7).AddHours(11), recurrence, member)).ToArray();
        db.ClassSessions.AddRange(sessions);
        await db.SaveChangesAsync();

        var result = await Apply(db, new() { GroupIds = [sessions[1].Id], Scope = scope, Operation = CalendarBulkOperation.Delete });

        Assert.True(result.Succeeded, result.Message);
        Assert.Equal(remaining, await db.ClassSessions.CountAsync());
        if (remaining == 1) Assert.Equal(sessions[0].Id, (await db.ClassSessions.SingleAsync()).Id);
    }

    [Fact]
    public async Task LegacyGroup_ExpandsOnlyMatchingNullIdentityGroupWeekdayTimeAndCapacity()
    {
        await using var db = Db();
        var (trainer, member, monday) = await Seed(db);
        var group = Group(trainer);
        var first = Session(group, monday.AddHours(11), null, member);
        var second = Session(group, monday.AddDays(7).AddHours(11), null, member);
        first.CapacityOverride = second.CapacityOverride = 6;
        var differentDay = Session(group, monday.AddDays(2).AddHours(11), null, member);
        var differentTime = Session(group, monday.AddDays(14).AddHours(12), null, member);
        var differentCapacity = Session(group, monday.AddDays(21).AddHours(11), null, member);
        var differentGroup = Session(Group(trainer), monday.AddDays(28).AddHours(11), null, member);
        var identified = Session(group, monday.AddDays(35).AddHours(11), Guid.NewGuid(), member);
        foreach (var item in new[] { differentDay, differentTime, differentGroup, identified }) item.CapacityOverride = 6;
        differentCapacity.CapacityOverride = 7;
        var untouched = new[] { differentDay, differentTime, differentCapacity, differentGroup, identified };
        db.ClassSessions.AddRange(untouched.Append(first).Append(second));
        await db.SaveChangesAsync();

        var result = await Apply(db, new() { GroupIds = [first.Id], Scope = CalendarBulkScope.AllUpcomingOccurrences,
            Operation = CalendarBulkOperation.Delete });

        Assert.True(result.Succeeded, result.Message);
        Assert.Equal(untouched.Select(item => item.Id).Order(), (await db.ClassSessions.Select(item => item.Id).ToListAsync()).Order());
    }

    [Fact]
    public async Task GroupUpdate_LaterParticipantConflictWithOtherTrainerRollsBackWholeSeries()
    {
        await using var db = Db();
        var (trainer, member, monday) = await Seed(db);
        var other = Member(trainer, member.MembershipPackage);
        var recurrence = Guid.NewGuid();
        var group = Group(trainer);
        var first = Session(group, monday.AddHours(11), recurrence, member);
        var second = Session(group, monday.AddDays(7).AddHours(11), recurrence, other);
        var conflict = Personal(Trainer(), other, monday.AddDays(11).AddHours(15));
        db.ClassSessions.AddRange(first, second);
        db.PersonalTrainingSessions.Add(conflict);
        await db.SaveChangesAsync();
        var before = new[] { first.StartsAtUtc, second.StartsAtUtc };
        var credits = new[] { member.RemainingClassCredits, other.RemainingClassCredits };

        var result = await Apply(db, new() { GroupIds = [first.Id], MemberId = member.Id,
            Scope = CalendarBulkScope.AllUpcomingOccurrences, Operation = CalendarBulkOperation.UpdateTime,
            Day = DayOfWeek.Friday, Time = new(15, 0) });

        Assert.False(result.Succeeded);
        Assert.Equal(before, new[] { first.StartsAtUtc, second.StartsAtUtc });
        Assert.Equal(before, (await db.ClassSessions.AsNoTracking().OrderBy(item => item.Id).Select(item => item.StartsAtUtc).ToListAsync()).ToArray());
        Assert.Equal(credits, new[] { member.RemainingClassCredits, other.RemainingClassCredits });
        Assert.All(new[] { first, second }, item => Assert.Null(item.UpdatedAtUtc));
        Assert.Empty(db.PersonalTrainingSessionHistories);
    }

    [Fact]
    public async Task GroupUpdate_LaterParticipantPackageEndRollsBackWholeSeries()
    {
        await using var db = Db();
        var (trainer, member, monday) = await Seed(db);
        var other = Member(trainer, member.MembershipPackage);
        var recurrence = Guid.NewGuid();
        var group = Group(trainer);
        var first = Session(group, monday.AddHours(11), recurrence, member);
        var second = Session(group, monday.AddDays(7).AddHours(11), recurrence, other);
        other.MembershipEndsAtUtc = second.StartsAtUtc.AddHours(2);
        db.ClassSessions.AddRange(first, second);
        await db.SaveChangesAsync();
        var before = new[] { first.StartsAtUtc, second.StartsAtUtc };

        var result = await Apply(db, new() { GroupIds = [first.Id], MemberId = member.Id,
            Scope = CalendarBulkScope.AllUpcomingOccurrences, Operation = CalendarBulkOperation.UpdateTime,
            Day = DayOfWeek.Friday });

        Assert.False(result.Succeeded);
        Assert.Equal(before, new[] { first.StartsAtUtc, second.StartsAtUtc });
        Assert.All(new[] { first, second }, item => Assert.Null(item.UpdatedAtUtc));
        Assert.All(first.Reservations.Concat(second.Reservations), item => Assert.Equal(ClassReservationStatus.Reserved, item.Status));
    }

    [Fact]
    public async Task PersonalUpdate_MoveAcrossMonthCannotExceedMonthlyPackageQuota()
    {
        await using var db = Db();
        var (trainer, member, _) = await Seed(db);
        var month = new DateTime(ClubTime.Now.Year, ClubTime.Now.Month, 1).AddMonths(2);
        while (month.DayOfWeek == DayOfWeek.Monday) month = month.AddMonths(1);
        var sunday = ClubTime.Monday(month).AddDays(6).AddHours(11);
        var recurrence = Guid.NewGuid();
        var first = Personal(trainer, member, sunday, recurrence);
        var second = Personal(trainer, member, sunday.AddDays(7), recurrence);
        var previousMonth = month.AddMonths(-1);
        var completed = Personal(trainer, member, previousMonth.AddDays(10).AddHours(9));
        completed.Status = PersonalTrainingSessionStatus.Completed;
        var noShow = Personal(trainer, member, previousMonth.AddDays(17).AddHours(9));
        noShow.Status = PersonalTrainingSessionStatus.NoShow;
        member.ServicePackageVariant = new() { Name = "Monthly PT", PersonalTrainingSessionCount = 2, LessonsRenewMonthly = true,
            ServicePackage = new() { Name = "PT", Slug = "monthly-pt-test", Category = ServicePackageCategory.PersonalTraining } };
        db.PersonalTrainingSessions.AddRange(first, second, completed, noShow);
        await db.SaveChangesAsync();
        var before = new[] { first.StartsAtUtc, second.StartsAtUtc };

        var result = await Apply(db, new() { PersonalIds = [first.Id], Scope = CalendarBulkScope.AllUpcomingOccurrences,
            Operation = CalendarBulkOperation.UpdateTime, Day = DayOfWeek.Monday });

        Assert.False(result.Succeeded);
        Assert.Equal(before, new[] { first.StartsAtUtc, second.StartsAtUtc });
        Assert.Empty(db.PersonalTrainingSessionHistories);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task RecurringScope_RejectsForeignTrainerAnchorBeforeExpansion(bool groupAnchor)
    {
        await using var db = Db();
        var (trainer, member, monday) = await Seed(db);
        var foreignTrainer = Trainer();
        var recurrence = Guid.NewGuid();
        var own = Personal(trainer, member, monday.AddHours(11), recurrence);
        var foreign = Personal(foreignTrainer, member, monday.AddDays(7).AddHours(11), recurrence);
        var foreignGroup = Session(Group(foreignTrainer), monday.AddDays(14).AddHours(11), recurrence, member);
        db.PersonalTrainingSessions.AddRange(own, foreign);
        db.ClassSessions.Add(foreignGroup);
        await db.SaveChangesAsync();

        var result = await new CalendarBulkOperationService(db).ApplyAsync(new() {
            PersonalIds = groupAnchor ? [] : [foreign.Id], GroupIds = groupAnchor ? [foreignGroup.Id] : [],
            Scope = CalendarBulkScope.AllUpcomingOccurrences, Operation = CalendarBulkOperation.Delete,
            TrainerId = foreignTrainer.Id }, "trainer", trainer.Id);

        Assert.False(result.Succeeded);
        Assert.Equal(2, await db.PersonalTrainingSessions.CountAsync());
        Assert.Single(db.ClassSessions);
        Assert.Equal(1000, member.RemainingClassCredits);
    }

    [Fact]
    public async Task GroupScope_RejectsAnchorWithoutSelectedMemberEvenIfLaterOccurrenceContainsMember()
    {
        await using var db = Db();
        var (trainer, member, monday) = await Seed(db);
        var other = Member(trainer, member.MembershipPackage);
        var recurrence = Guid.NewGuid();
        var group = Group(trainer);
        var first = Session(group, monday.AddHours(11), recurrence, other);
        var second = Session(group, monday.AddDays(7).AddHours(11), recurrence, member);
        db.ClassSessions.AddRange(first, second);
        await db.SaveChangesAsync();

        var result = await Apply(db, new() { GroupIds = [first.Id], MemberId = member.Id,
            Scope = CalendarBulkScope.AllUpcomingOccurrences, Operation = CalendarBulkOperation.Delete });

        Assert.False(result.Succeeded);
        Assert.Equal(2, await db.ClassSessions.CountAsync());
        Assert.Equal(1000, member.RemainingClassCredits);
        Assert.Equal(1000, other.RemainingClassCredits);
    }

    [Theory]
    [InlineData("missing-change")]
    [InlineData("invalid-day")]
    [InlineData("invalid-scope")]
    [InlineData("member-range-and-series")]
    public async Task InvalidRecurrenceInput_DoesNotChangeAnySession(string invalid)
    {
        await using var db = Db();
        var (trainer, member, monday) = await Seed(db);
        var first = Personal(trainer, member, monday.AddHours(11), Guid.NewGuid());
        db.PersonalTrainingSessions.Add(first);
        await db.SaveChangesAsync();
        var before = first.StartsAtUtc;
        var input = new CalendarBulkInput { PersonalIds = [first.Id], Scope = CalendarBulkScope.AllUpcomingOccurrences,
            Operation = CalendarBulkOperation.UpdateTime, Day = DayOfWeek.Wednesday };
        if (invalid == "missing-change") input.Day = null;
        if (invalid == "invalid-day") input.Day = (DayOfWeek)7;
        if (invalid == "invalid-scope") input.Scope = (CalendarBulkScope)99;
        if (invalid == "member-range-and-series")
        {
            input.AllMemberLessons = true;
            input.MemberId = member.Id;
            input.From = DateOnly.FromDateTime(monday);
            input.Until = input.From.AddMonths(1);
        }

        var result = await Apply(db, input);

        Assert.False(result.Succeeded);
        Assert.Equal(before, first.StartsAtUtc);
        Assert.Equal(before, (await db.PersonalTrainingSessions.AsNoTracking().SingleAsync()).StartsAtUtc);
        Assert.Null(first.UpdatedAtUtc);
        Assert.Empty(db.PersonalTrainingSessionHistories);
    }

    [Theory]
    [InlineData(249, true)]
    [InlineData(250, false)]
    public async Task MixedSeriesExpansion_Enforces500TotalLimitWithoutPartialDeletion(int groupCount, bool succeeds)
    {
        await using var db = Db();
        var (trainer, member, monday) = await Seed(db);
        var personalRecurrence = Guid.NewGuid();
        var groupRecurrence = Guid.NewGuid();
        var group = Group(trainer);
        var personal = Enumerable.Range(0, 251).Select(index => Personal(trainer, member,
            monday.AddDays(index * 7).AddHours(11), personalRecurrence)).ToArray();
        var groups = Enumerable.Range(0, groupCount).Select(index => Session(group,
            monday.AddDays(index * 7 + 2).AddHours(11), groupRecurrence, member)).ToArray();
        db.PersonalTrainingSessions.AddRange(personal);
        db.ClassSessions.AddRange(groups);
        await db.SaveChangesAsync();

        var result = await Apply(db, new() { PersonalIds = [personal[0].Id], GroupIds = [groups[0].Id],
            Scope = CalendarBulkScope.AllUpcomingOccurrences, Operation = CalendarBulkOperation.Delete });

        Assert.Equal(succeeds, result.Succeeded);
        Assert.Equal(succeeds ? 0 : 251, await db.PersonalTrainingSessions.CountAsync());
        Assert.Equal(succeeds ? 0 : groupCount, await db.ClassSessions.CountAsync());
        Assert.Equal(succeeds ? 0 : groupCount, await db.ClassReservations.CountAsync());
        Assert.Equal(succeeds ? 1000 + groupCount : 1000, member.RemainingClassCredits);
        if (!succeeds) Assert.All(groups.SelectMany(item => item.Reservations), item => Assert.Equal(ClassReservationStatus.Reserved, item.Status));
    }

    private static Task<(bool Succeeded, string Message)> Apply(ApplicationDbContext db, CalendarBulkInput input) =>
        new CalendarBulkOperationService(db).ApplyAsync(input, "admin");

    private static ApplicationDbContext Db() => new(new DbContextOptionsBuilder<ApplicationDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private static Trainer Trainer() => new() { FirstName = "Test", LastName = "Coach", Specialty = "PT" };

    private static MemberProfile Member(Trainer trainer, MembershipPackage package) => new() {
        AssignedTrainer = trainer, ApplicationUser = new() { UserName = Guid.NewGuid().ToString() }, MembershipPackage = package,
        RemainingClassCredits = 1000, MembershipStartsAtUtc = DateTime.UtcNow.AddYears(-1), MembershipEndsAtUtc = DateTime.UtcNow.AddYears(15)
    };

    private static async Task<(Trainer Trainer, MemberProfile Member, DateTime Monday)> Seed(ApplicationDbContext db)
    {
        var trainer = Trainer();
        var member = Member(trainer, new() { Name = "Test", WeeklyClassLimit = 4 });
        db.MemberProfiles.Add(member);
        await db.SaveChangesAsync();
        return (trainer, member, ClubTime.Monday(ClubTime.Now.AddDays(14)));
    }

    private static PersonalTrainingSession Personal(Trainer trainer, MemberProfile member, DateTime localStart, Guid? recurrence = null) => new() {
        Trainer = trainer, MemberProfile = member, StartsAtUtc = ClubTime.ToUtc(localStart), RecurrenceId = recurrence, DurationMinutes = 50
    };

    private static GroupClass Group(Trainer trainer) => new() { Trainer = trainer, Name = "Weekly group", DurationMinutes = 60, Capacity = 8 };

    private static ClassSession Session(GroupClass group, DateTime localStart, Guid? recurrence, params MemberProfile[] members) => new() {
        GroupClass = group, StartsAtUtc = ClubTime.ToUtc(localStart), RecurrenceId = recurrence,
        Reservations = members.Select(member => new ClassReservation { MemberProfile = member }).ToList()
    };
}
