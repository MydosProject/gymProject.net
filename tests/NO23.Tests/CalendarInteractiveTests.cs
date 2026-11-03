using Microsoft.EntityFrameworkCore;
using NO23.Web.Data;
using NO23.Web.Domain.Entities;
using NO23.Web.Domain.Enums;
using NO23.Web.Services;
using NO23.Web.ViewModels;
using NO23.Web.ViewModels.Admin;

namespace NO23.Tests;

public class CalendarInteractiveTests
{
    [Fact]
    public async Task Feed_ReturnsKindsEditabilityAndPackageProgress()
    {
        await using var db = Db();
        var now = DateTime.UtcNow;
        var trainer = new Trainer { FirstName = "Deniz", LastName = "Coach", Specialty = "PT" };
        var member = PackageMember(trainer, ptCredits: 8);
        db.Add(member);
        db.PersonalTrainingSessions.AddRange(
            new PersonalTrainingSession { Trainer = trainer, MemberProfile = member, StartsAtUtc = now.AddDays(-2), DurationMinutes = 50, Status = PersonalTrainingSessionStatus.Completed },
            new PersonalTrainingSession { Trainer = trainer, MemberProfile = member, StartsAtUtc = now.AddDays(2), DurationMinutes = 50 },
            new PersonalTrainingSession { Trainer = trainer, MemberProfile = member, StartsAtUtc = now.AddDays(3), DurationMinutes = 60 });
        var group = new ClassSession { StartsAtUtc = now.AddDays(1), GroupClass = new() { Name = "Bootcamp", Trainer = trainer, Capacity = 10, DurationMinutes = 45, IsActive = true } };
        group.Reservations.Add(new ClassReservation { MemberProfile = member });
        db.ClassSessions.Add(group);
        db.AppointmentRequests.Add(new AppointmentRequest { FullName = "Aday Üye", Service = "Ön görüşme", ScheduledTrainer = trainer,
            ScheduledAtUtc = now.AddDays(4), DurationMinutes = 30, ScheduleStatus = AppointmentScheduleStatus.Scheduled });
        await db.SaveChangesAsync();
        var service = new CalendarEventFeedService(db);

        var events = await service.GetAsync(new(now.AddDays(-7), now.AddDays(7), null, null, true), now);

        Assert.Equal(5, events.Count);
        var completed = events.Single(x => x.Kind == "personal" && x.IsDone);
        Assert.False(completed.StartEditable);
        var next = events.Where(x => x.Kind == "personal" && !x.IsDone).OrderBy(x => x.Start).First();
        Assert.True(next.StartEditable);
        Assert.True(next.DurationEditable);
        Assert.Equal("Member Test", next.Title);
        Assert.Contains("2. ders / 8", next.Progress);
        Assert.Contains("Kalan 8", next.Progress);
        var groupEvent = events.Single(x => x.Kind == "group");
        Assert.True(groupEvent.StartEditable);
        Assert.False(groupEvent.DurationEditable);
        Assert.Equal((1, 10), (groupEvent.ReservedCount, groupEvent.Capacity));
        Assert.Equal(["Member Test"], groupEvent.Participants);
        Assert.False(events.Single(x => x.Kind == "consultation").StartEditable);
        Assert.Equal(ClubTime.ToLocal(group.StartsAtUtc).ToString("yyyy-MM-ddTHH:mm:ss"), groupEvent.Start);

        Assert.Equal(2, (await service.GetAsync(new(now.AddDays(-7), now.AddDays(7), null, null, false), now)).Count(x => x.Kind == "personal"));
        Assert.DoesNotContain(await service.GetAsync(new(now.AddDays(-7), now.AddDays(7), null, member.Id, true), now), x => x.Kind == "consultation");
        Assert.Empty(await service.GetAsync(new(now.AddDays(-7), now.AddDays(7), trainer.Id + 100, null, true), now));
    }

    [Theory]
    [InlineData("2026-11-02T00:00:00+03:00", "2026-11-09T00:00:00+03:00", true)]
    [InlineData("2026-11-02T00:00:00", "2026-11-09T00:00:00", true)]
    [InlineData("2026-11-02T00:00:00+03:00", "2027-01-20T00:00:00+03:00", false)]
    [InlineData("2026-11-09T00:00:00+03:00", "2026-11-02T00:00:00+03:00", false)]
    [InlineData("yarın", "2026-11-09T00:00:00+03:00", false)]
    public void TryParseRange_AcceptsBrowserOffsetsAndClubTime(string start, string end, bool valid)
    {
        Assert.Equal(valid, CalendarEventFeedService.TryParseRange(start, end, out var fromUtc, out _));
        if (valid) Assert.Equal(new DateTime(2026, 11, 1, 21, 0, 0, DateTimeKind.Utc), fromUtc);
    }

    [Fact]
    public async Task MovePersonal_ChangesTimeAndDurationWithHistory_AndKeepsCredit()
    {
        await using var db = Db();
        var (trainer, member) = await SeedLegacyAsync(db);
        var personal = new PersonalTrainingCalendarService(db);
        Assert.True((await personal.CreateAsync(trainer.Id, member.Id, DateTime.UtcNow.AddDays(2), 50, null)).Succeeded);
        var session = await db.PersonalTrainingSessions.SingleAsync();
        var target = ClubTime.ToLocal(session.StartsAtUtc).AddDays(1).AddHours(1);

        var result = await new CalendarMoveService(db, personal).MoveAsync(
            new() { Kind = CalendarMoveInput.Personal, Id = session.Id, Start = target, DurationMinutes = 60 }, "admin", null);

        Assert.True(result.Succeeded, result.Message);
        Assert.Equal(ClubTime.ToUtc(target), session.StartsAtUtc);
        Assert.Equal(60, session.DurationMinutes);
        Assert.Equal(PersonalTrainingSessionStatus.Scheduled, session.Status);
        Assert.Equal(4, member.RemainingClassCredits);
        Assert.Contains("süre 50 → 60", (await db.PersonalTrainingSessionHistories.SingleAsync()).Note);

        var resized = await new CalendarMoveService(db, personal).MoveAsync(
            new() { Kind = CalendarMoveInput.Personal, Id = session.Id, Start = target, DurationMinutes = 75 }, "admin", null);
        Assert.Equal("Ders süresi 75 dakika olarak güncellendi; ders hakkı değişmedi.", resized.Message);
        Assert.Equal(ClubTime.ToUtc(target), session.StartsAtUtc);
        Assert.Equal(75, session.DurationMinutes);
    }

    [Theory]
    [InlineData("other-trainer")]
    [InlineData("conflict")]
    [InlineData("past")]
    [InlineData("too-long")]
    public async Task MovePersonal_RefusedMoveChangesNothing(string problem)
    {
        await using var db = Db();
        var (trainer, member) = await SeedLegacyAsync(db);
        var personal = new PersonalTrainingCalendarService(db);
        Assert.True((await personal.CreateAsync(trainer.Id, member.Id, DateTime.UtcNow.AddDays(2), 50, null)).Succeeded);
        var session = await db.PersonalTrainingSessions.SingleAsync();
        var before = session.StartsAtUtc;
        var target = ClubTime.ToLocal(before).AddDays(1);
        int? scope = null;
        int? duration = null;
        if (problem == "other-trainer") scope = trainer.Id + 100;
        if (problem == "conflict")
        {
            db.ClassSessions.Add(new ClassSession { StartsAtUtc = ClubTime.ToUtc(target), GroupClass = new() { Name = "Metcon", Trainer = trainer, Capacity = 8, DurationMinutes = 45, IsActive = true } });
            await db.SaveChangesAsync();
        }
        if (problem == "past") target = ClubTime.Now.AddHours(-3);
        if (problem == "too-long") duration = 300;

        var result = await new CalendarMoveService(db, personal).MoveAsync(
            new() { Kind = CalendarMoveInput.Personal, Id = session.Id, Start = target, DurationMinutes = duration }, "admin", scope);

        Assert.False(result.Succeeded);
        Assert.Equal(before, session.StartsAtUtc);
        Assert.Equal(50, session.DurationMinutes);
        Assert.Empty(db.PersonalTrainingSessionHistories);
    }

    [Fact]
    public async Task MoveGroup_CanGoToAnotherWeek_ButNotPastParticipantsPackageOrChangeDuration()
    {
        await using var db = Db();
        var (trainer, member) = await SeedLegacyAsync(db);
        var monday = ClubTime.Monday(ClubTime.Now.AddDays(14));
        var group = new ClassSession { StartsAtUtc = ClubTime.ToUtc(monday.AddHours(18)), GroupClass = new() { Name = "Bootcamp", Trainer = trainer, Capacity = 10, DurationMinutes = 45, IsActive = true } };
        db.ClassSessions.Add(group);
        await db.SaveChangesAsync();
        var mover = new CalendarMoveService(db, new PersonalTrainingCalendarService(db));
        var nextWeek = monday.AddDays(9).AddHours(19);

        Assert.False((await mover.MoveAsync(new() { Kind = CalendarMoveInput.Group, Id = group.Id, Start = nextWeek, DurationMinutes = 60 }, "admin", null)).Succeeded);
        var moved = await mover.MoveAsync(new() { Kind = CalendarMoveInput.Group, Id = group.Id, Start = nextWeek }, "admin", null);
        Assert.True(moved.Succeeded, moved.Message);
        Assert.Equal(ClubTime.ToUtc(nextWeek), group.StartsAtUtc);

        member.MembershipEndsAtUtc = ClubTime.ToUtc(nextWeek.AddDays(2));
        group.Reservations.Add(new ClassReservation { MemberProfile = member });
        await db.SaveChangesAsync();
        var refused = await mover.MoveAsync(new() { Kind = CalendarMoveInput.Group, Id = group.Id, Start = nextWeek.AddDays(7) }, "admin", null);
        Assert.False(refused.Succeeded);
        Assert.Equal(ClubTime.ToUtc(nextWeek), group.StartsAtUtc);
        Assert.False((await mover.MoveAsync(new() { Kind = CalendarMoveInput.Group, Id = group.Id, Start = nextWeek.AddDays(1) }, "admin", trainer.Id + 100)).Succeeded);
    }

    [Fact]
    public async Task BulkDate_IsOnlyForASingleLesson()
    {
        await using var db = Db();
        var (trainer, member) = await SeedLegacyAsync(db);
        var personal = new PersonalTrainingCalendarService(db);
        Assert.True((await personal.CreateAsync(trainer.Id, member.Id, DateTime.UtcNow.AddDays(2), 50, null)).Succeeded);
        Assert.True((await personal.CreateAsync(trainer.Id, member.Id, DateTime.UtcNow.AddDays(3), 50, null)).Succeeded);
        var ids = await db.PersonalTrainingSessions.Select(x => x.Id).ToArrayAsync();

        var result = await new CalendarBulkOperationService(db).ApplyAsync(new CalendarBulkInput
        {
            PersonalIds = ids, Operation = CalendarBulkOperation.UpdateTime, Date = DateOnly.FromDateTime(ClubTime.Now.AddDays(10)), Time = new(10, 0)
        }, "admin");

        Assert.False(result.Succeeded);
        Assert.Equal("Belirli bir tarihe yalnız tek ders taşınabilir.", result.Message);
    }

    private static MemberProfile PackageMember(Trainer trainer, int ptCredits)
    {
        var variant = new ServicePackageVariant { Name = $"{ptCredits} Ders", PersonalTrainingSessionCount = ptCredits,
            ServicePackage = new() { Slug = Guid.NewGuid().ToString(), Name = "PT FLEX", Category = ServicePackageCategory.PersonalTraining } };
        var member = new MemberProfile { ApplicationUser = new() { UserName = Guid.NewGuid().ToString(), FirstName = "Member", LastName = "Test" },
            MembershipPackage = new() { Code = MembershipPackageCode.Start, Name = "Start" }, AssignedTrainer = trainer, ServicePackageVariant = variant,
            MembershipStartsAtUtc = DateTime.UtcNow.AddDays(-10), MembershipEndsAtUtc = DateTime.UtcNow.AddMonths(2) };
        CategoryCreditService.Initialize(member, variant, "test-purchase");
        return member;
    }

    private static async Task<(Trainer Trainer, MemberProfile Member)> SeedLegacyAsync(ApplicationDbContext db)
    {
        var trainer = new Trainer { FirstName = "Test", LastName = "Trainer", Specialty = "PT" };
        var member = new MemberProfile
        {
            ApplicationUser = new ApplicationUser { UserName = "member@test.local", Email = "member@test.local" },
            MembershipPackage = new MembershipPackage { Code = MembershipPackageCode.Pro, Name = "Pro", Audience = "Test", Description = "Test", WeeklyClassLimit = 4 },
            MembershipPackageOption = new MembershipPackageOption { Name = "4 PT", PersonalTrainingSessionCount = 4 },
            MembershipStartsAtUtc = DateTime.UtcNow.AddYears(-2),
            AssignedTrainer = trainer,
            RemainingClassCredits = 4
        };
        db.Add(member);
        await db.SaveChangesAsync();
        return (trainer, member);
    }

    private static ApplicationDbContext Db() => new(new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
}
