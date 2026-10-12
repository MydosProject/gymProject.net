using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NO23.Web.Data;
using NO23.Web.Domain.Entities;
using NO23.Web.Domain.Enums;
using NO23.Web.Services;
using NO23.Web.ViewModels.Admin;
using System.Security.Claims;

namespace NO23.Tests;

public class CalendarBulkOperationTests
{
    [Fact]
    public async Task UpdateTime_PreservesLocalDatesCreditsAndWritesHistory()
    {
        await using var db = Db();
        var (trainer, member, first, second) = await Seed(db);
        var dates = new[] { ClubTime.ToLocal(first.StartsAtUtc).Date, ClubTime.ToLocal(second.StartsAtUtc).Date };
        var result = await new CalendarBulkOperationService(db).ApplyAsync(new() {
            PersonalIds = [first.Id, second.Id], MemberId = member.Id, Operation = CalendarBulkOperation.UpdateTime, Time = new(10, 30)
        }, "admin");
        Assert.True(result.Succeeded, result.Message);
        Assert.Equal(dates, new[] { ClubTime.ToLocal(first.StartsAtUtc).Date, ClubTime.ToLocal(second.StartsAtUtc).Date });
        Assert.All(new[] { first, second }, item => Assert.Equal(new TimeOnly(10, 30), TimeOnly.FromDateTime(ClubTime.ToLocal(item.StartsAtUtc))));
        Assert.Equal(4, member.RemainingClassCredits);
        Assert.Equal(2, await db.PersonalTrainingSessionHistories.CountAsync());
        Assert.All(db.PersonalTrainingSessionHistories, item => Assert.Equal("admin", item.ChangedByUserId));
    }

    [Theory]
    [InlineData("missing")]
    [InlineData("other-trainer")]
    [InlineData("other-member")]
    [InlineData("completed")]
    [InlineData("missing-time")]
    [InlineData("outside-period")]
    [InlineData("overlap")]
    public async Task InvalidBatch_ChangesNothing(string problem)
    {
        await using var db = Db();
        var (trainer, member, first, second) = await Seed(db);
        var input = new CalendarBulkInput { PersonalIds = [first.Id, second.Id], Operation = CalendarBulkOperation.UpdateTime, Time = new(10, 0) };
        int? scope = null;
        if (problem == "missing") input.PersonalIds = [first.Id, int.MaxValue];
        if (problem == "other-trainer") scope = trainer.Id + 100;
        if (problem == "other-member") input.MemberId = member.Id + 100;
        if (problem == "completed") second.Status = PersonalTrainingSessionStatus.Completed;
        if (problem == "missing-time") input.Time = null;
        if (problem == "outside-period") member.MembershipEndsAtUtc = first.StartsAtUtc.AddHours(-2);
        if (problem == "overlap") db.PersonalTrainingSessions.Add(new() { Trainer = trainer, MemberProfile = member,
            StartsAtUtc = ClubTime.ToUtc(ClubTime.ToLocal(first.StartsAtUtc).Date.AddHours(10)), DurationMinutes = 50 });
        await db.SaveChangesAsync();
        var before = new[] { first.StartsAtUtc, second.StartsAtUtc };
        var result = await new CalendarBulkOperationService(db).ApplyAsync(input, "admin", scope);
        Assert.False(result.Succeeded);
        Assert.Equal(before, new[] { first.StartsAtUtc, second.StartsAtUtc });
        Assert.Empty(db.PersonalTrainingSessionHistories);
        Assert.Equal(4, member.RemainingClassCredits);
    }

    [Fact]
    public async Task UpdateGroup_ValidatesAllParticipantsAndTheirOtherTrainerLessons()
    {
        await using var db = Db();
        var (trainer, member, first, _) = await Seed(db);
        var otherTrainer = new Trainer { FirstName = "Other", LastName = "Coach", Specialty = "PT" };
        var group = new ClassSession { GroupClass = new GroupClass { Trainer = trainer, Name = "Group", DurationMinutes = 60 },
            StartsAtUtc = first.StartsAtUtc.AddHours(4), Reservations = [new() { MemberProfile = member }] };
        db.ClassSessions.Add(group);
        db.PersonalTrainingSessions.Add(new() { Trainer = otherTrainer, MemberProfile = member,
            StartsAtUtc = ClubTime.ToUtc(ClubTime.ToLocal(first.StartsAtUtc).Date.AddHours(10)), DurationMinutes = 60 });
        await db.SaveChangesAsync();
        var original = group.StartsAtUtc;
        var result = await new CalendarBulkOperationService(db).ApplyAsync(new() {
            GroupIds = [group.Id], Operation = CalendarBulkOperation.UpdateTime, Time = new(10, 0)
        }, "admin");
        Assert.False(result.Succeeded);
        Assert.Equal(original, group.StartsAtUtc);
    }

    [Fact]
    public async Task DeleteMixedBatch_RemovesLessonsAndRefundsGroupReservationExactlyOnce()
    {
        await using var db = Db();
        var (trainer, member, first, second) = await Seed(db);
        var group = new ClassSession { GroupClass = new GroupClass { Trainer = trainer, Name = "Group", DurationMinutes = 60 },
            StartsAtUtc = first.StartsAtUtc.AddDays(1), Reservations = [new() { MemberProfile = member }] };
        db.ClassSessions.Add(group);
        await db.SaveChangesAsync();
        var input = new CalendarBulkInput { PersonalIds = [first.Id], GroupIds = [group.Id], Operation = CalendarBulkOperation.Delete };
        var result = await new CalendarBulkOperationService(db).ApplyAsync(input, "admin");
        Assert.True(result.Succeeded, result.Message);
        Assert.Equal(5, member.RemainingClassCredits);
        Assert.Equal(second.Id, (await db.PersonalTrainingSessions.SingleAsync()).Id);
        Assert.Empty(db.ClassSessions);
        Assert.Empty(db.ClassReservations);
        Assert.False((await new CalendarBulkOperationService(db).ApplyAsync(input, "admin")).Succeeded);
        Assert.Equal(5, member.RemainingClassCredits);
    }

    [Fact]
    public async Task MemberRange_AppliesAcrossWeeksAndKeepsOtherMembersAndDatesOutsideRange()
    {
        await using var db = Db();
        var (trainer, member, first, second) = await Seed(db);
        var outside = new PersonalTrainingSession { Trainer = trainer, MemberProfile = member, StartsAtUtc = first.StartsAtUtc.AddDays(28) };
        db.PersonalTrainingSessions.Add(outside);
        await db.SaveChangesAsync();
        var result = await new CalendarBulkOperationService(db).ApplyAsync(new() { AllMemberLessons = true, MemberId = member.Id,
            From = DateOnly.FromDateTime(ClubTime.ToLocal(first.StartsAtUtc)), Until = DateOnly.FromDateTime(ClubTime.ToLocal(second.StartsAtUtc)),
            Operation = CalendarBulkOperation.Delete }, "admin");
        Assert.True(result.Succeeded, result.Message);
        Assert.Equal(outside.Id, (await db.PersonalTrainingSessions.SingleAsync()).Id);
        Assert.Equal(4, member.RemainingClassCredits);
    }

    [Fact]
    public async Task CalendarMemberFilter_ExcludesOtherMembersAndInactiveGroups()
    {
        await using var db = Db();
        var (trainer, member, first, second) = await Seed(db);
        var other = new MemberProfile { ApplicationUser = new() { UserName = "other", FirstName = "Other" }, MembershipPackage = member.MembershipPackage };
        db.PersonalTrainingSessions.Add(new() { Trainer = trainer, MemberProfile = other, StartsAtUtc = first.StartsAtUtc.AddHours(3) });
        var active = new ClassSession { GroupClass = new() { Trainer = trainer, Name = "Visible", DurationMinutes = 60 },
            StartsAtUtc = first.StartsAtUtc.AddDays(1), Reservations = [new() { MemberProfile = member }] };
        var inactive = new ClassSession { GroupClass = new() { Trainer = trainer, Name = "Hidden", IsActive = false, DurationMinutes = 60 },
            StartsAtUtc = first.StartsAtUtc.AddDays(1), Reservations = [new() { MemberProfile = member }] };
        var unrelated = new ClassSession { GroupClass = new() { Trainer = trainer, Name = "Other group", DurationMinutes = 60 },
            StartsAtUtc = first.StartsAtUtc.AddDays(1), Reservations = [new() { MemberProfile = other }] };
        db.ClassSessions.AddRange(active, inactive, unrelated);
        await db.SaveChangesAsync();
        var controller = new NO23.Web.Areas.Admin.Controllers.CalendarController(db, new(db), new(db));
        var view = Assert.IsType<ViewResult>(await controller.Index(ClubTime.ToLocal(first.StartsAtUtc), null, member.Id));
        var page = Assert.IsType<WeeklyCalendarViewModel>(view.Model);
        Assert.Equal(member.Id, page.MemberId);
        Assert.Equal(new[] { first.Id, second.Id }, page.Personal.Select(item => item.Id));
        Assert.Equal(active.Id, Assert.Single(page.Groups).Id);
        var trainerController = new NO23.Web.Areas.Trainer.Controllers.CalendarController(db, new(db)) {
            ControllerContext = new() { HttpContext = new DefaultHttpContext { User = new(new ClaimsIdentity([new(ClaimTypes.NameIdentifier, trainer.ApplicationUserId!)], "test")) } }
        };
        var trainerView = Assert.IsType<ViewResult>(await trainerController.Index(ClubTime.ToLocal(first.StartsAtUtc), member.Id));
        var trainerPage = Assert.IsType<NO23.Web.ViewModels.TrainerPanel.TrainerCalendarViewModel>(trainerView.Model);
        Assert.Equal(2, trainerPage.Sessions.Count);
        Assert.Equal(active.Id, Assert.Single(trainerPage.Groups).Id);
        var programController = new NO23.Web.Areas.Admin.Controllers.ClassSessionsController(db, new(db), null!);
        var programView = Assert.IsType<ViewResult>(await programController.Index());
        var program = Assert.IsAssignableFrom<IReadOnlyList<ClassSessionListItemViewModel>>(programView.Model);
        Assert.DoesNotContain(program, item => item.Id == inactive.Id);
        var historyView = Assert.IsType<ViewResult>(await programController.Index(includeHistory: true));
        Assert.Contains(Assert.IsAssignableFrom<IReadOnlyList<ClassSessionListItemViewModel>>(historyView.Model), item => item.Id == inactive.Id);
    }

    [Fact]
    public async Task MemberDateRange_RespectsSelectedTrainerAndCannotBroadenTrainerAuthorization()
    {
        await using var db = Db();
        var (trainer, member, first, second) = await Seed(db);
        var otherTrainer = new Trainer { FirstName = "Other", LastName = "Coach", Specialty = "PT" };
        var otherLesson = new PersonalTrainingSession { Trainer = otherTrainer, MemberProfile = member,
            StartsAtUtc = first.StartsAtUtc.AddDays(1), DurationMinutes = 50 };
        db.PersonalTrainingSessions.Add(otherLesson);
        await db.SaveChangesAsync();
        var result = await new CalendarBulkOperationService(db).ApplyAsync(new CalendarBulkInput {
            AllMemberLessons = true, MemberId = member.Id, TrainerId = otherTrainer.Id,
            From = DateOnly.FromDateTime(ClubTime.ToLocal(first.StartsAtUtc)),
            Until = DateOnly.FromDateTime(ClubTime.ToLocal(second.StartsAtUtc)),
            Operation = CalendarBulkOperation.UpdateTime, Time = new TimeOnly(17, 0)
        }, "trainer", trainer.Id);
        Assert.True(result.Succeeded, result.Message);
        Assert.Equal(new TimeOnly(17, 0), TimeOnly.FromDateTime(ClubTime.ToLocal(first.StartsAtUtc)));
        Assert.Equal(new TimeOnly(17, 0), TimeOnly.FromDateTime(ClubTime.ToLocal(second.StartsAtUtc)));
        Assert.Equal(new TimeOnly(11, 0), TimeOnly.FromDateTime(ClubTime.ToLocal(otherLesson.StartsAtUtc)));

        result = await new CalendarBulkOperationService(db).ApplyAsync(new CalendarBulkInput {
            AllMemberLessons = true, MemberId = member.Id, TrainerId = otherTrainer.Id,
            From = DateOnly.FromDateTime(ClubTime.ToLocal(first.StartsAtUtc)),
            Until = DateOnly.FromDateTime(ClubTime.ToLocal(second.StartsAtUtc)),
            Operation = CalendarBulkOperation.UpdateTime, Time = new TimeOnly(18, 0)
        }, "admin");
        Assert.True(result.Succeeded, result.Message);
        Assert.Equal(new TimeOnly(18, 0), TimeOnly.FromDateTime(ClubTime.ToLocal(otherLesson.StartsAtUtc)));
        Assert.Equal(new TimeOnly(17, 0), TimeOnly.FromDateTime(ClubTime.ToLocal(first.StartsAtUtc)));
    }

    private static ApplicationDbContext Db() => new(new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
    private static async Task<(Trainer, MemberProfile, PersonalTrainingSession, PersonalTrainingSession)> Seed(ApplicationDbContext db)
    {
        var trainer = new Trainer { FirstName = "Test", LastName = "Coach", Specialty = "PT", ApplicationUser = new() { UserName = "coach" } };
        var member = new MemberProfile { AssignedTrainer = trainer, ApplicationUser = new() { UserName = "member", FirstName = "Member" },
            MembershipPackage = new() { Name = "Test", Audience = "Test", Description = "Test", WeeklyClassLimit = 4 }, RemainingClassCredits = 4,
            MembershipStartsAtUtc = DateTime.UtcNow.AddDays(-1), MembershipEndsAtUtc = DateTime.UtcNow.AddMonths(3) };
        var monday = ClubTime.Monday(ClubTime.Now.AddDays(14));
        var first = new PersonalTrainingSession { Trainer = trainer, MemberProfile = member, StartsAtUtc = ClubTime.ToUtc(monday.AddHours(11)), DurationMinutes = 50 };
        var second = new PersonalTrainingSession { Trainer = trainer, MemberProfile = member, StartsAtUtc = ClubTime.ToUtc(monday.AddDays(2).AddHours(14)), DurationMinutes = 50 };
        db.PersonalTrainingSessions.AddRange(first, second);
        await db.SaveChangesAsync();
        return (trainer, member, first, second);
    }
}
