using Microsoft.EntityFrameworkCore;
using NO23.Web.Data;
using NO23.Web.Domain.Entities;
using NO23.Web.Domain.Enums;
using NO23.Web.Services;

namespace NO23.Tests;

public class SessionCompletionServiceTests
{
    private static readonly DateTime Now = new(2026, 10, 14, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task AutomaticAndManualCompletion_ConsumeTheSameCreditAndRecordHistory()
    {
        await using var db = Db();
        var automatic = Lesson(Member("auto"));
        var manual = Lesson(Member("manual"));
        db.PersonalTrainingSessions.AddRange(automatic, manual);
        await db.SaveChangesAsync();

        Assert.True((await new PersonalTrainingCalendarService(db).ChangeStatusAsync(manual.TrainerId, manual.Id,
            PersonalTrainingSessionStatus.Completed, null, "trainer", "Ders yapıldı")).Succeeded);
        Assert.Equal(1, await new SessionCompletionService(db).CompleteDueAsync(Now));
        Assert.Equal(7, automatic.MemberProfile.RemainingClassCredits);
        Assert.Equal(manual.MemberProfile.RemainingClassCredits, automatic.MemberProfile.RemainingClassCredits);
        Assert.True(automatic.CreditConsumed);
        Assert.True(manual.CreditConsumed);
        var history = Assert.Single(automatic.History);
        Assert.Equal(SessionCompletionService.SystemActor, history.ChangedByUserId);
        Assert.Equal(Now, history.ChangedAtUtc);
        Assert.Equal(PersonalTrainingSessionStatus.Scheduled, history.PreviousStatus);
        Assert.Equal(PersonalTrainingSessionStatus.Completed, history.NewStatus);
        Assert.Equal(automatic.StartsAtUtc, history.PreviousStartsAtUtc);
        Assert.Equal(history.PreviousStartsAtUtc, history.NewStartsAtUtc);
        Assert.Single(manual.History);
    }

    [Fact]
    public async Task RepeatedAutomaticAndManualCompletion_DoesNotDebitOrRecordTwice()
    {
        await using var db = Db();
        var lesson = Lesson(Member("repeat"));
        db.PersonalTrainingSessions.Add(lesson);
        await db.SaveChangesAsync();
        var service = new SessionCompletionService(db);

        Assert.Equal(1, await service.CompleteDueAsync(Now));
        Assert.Equal(0, await service.CompleteDueAsync(Now.AddMinutes(1)));
        Assert.False((await new PersonalTrainingCalendarService(db).ChangeStatusAsync(lesson.TrainerId, lesson.Id,
            PersonalTrainingSessionStatus.Completed, null, "trainer", null)).Succeeded);
        Assert.Equal(7, lesson.MemberProfile.RemainingClassCredits);
        Assert.Single(await db.PersonalTrainingSessionHistories.ToListAsync());
    }

    [Theory]
    [InlineData(PersonalTrainingSessionStatus.Completed)]
    [InlineData(PersonalTrainingSessionStatus.NoShow)]
    public async Task ManualOutcomeThenAutomaticRun_DoesNotDebitAgain(PersonalTrainingSessionStatus status)
    {
        await using var db = Db();
        var lesson = Lesson(Member("manual-first"));
        db.PersonalTrainingSessions.Add(lesson);
        await db.SaveChangesAsync();

        Assert.True((await new PersonalTrainingCalendarService(db).ChangeStatusAsync(lesson.TrainerId, lesson.Id,
            status, null, "trainer", null)).Succeeded);
        Assert.Equal(0, await new SessionCompletionService(db).CompleteDueAsync(Now));
        Assert.Equal(7, lesson.MemberProfile.RemainingClassCredits);
        Assert.Single(lesson.History);
        Assert.Equal(status, lesson.Status);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CancellationThenAutomaticRun_PreservesOrRefundsTheCredit(bool alreadyConsumed)
    {
        await using var db = Db();
        var member = Member("cancel");
        member.RemainingClassCredits = alreadyConsumed ? 7 : 8;
        var lesson = Lesson(member);
        lesson.CreditConsumed = alreadyConsumed;
        db.PersonalTrainingSessions.Add(lesson);
        await db.SaveChangesAsync();

        Assert.True((await new PersonalTrainingCalendarService(db).ChangeStatusAsync(lesson.TrainerId, lesson.Id,
            PersonalTrainingSessionStatus.Cancelled, null, "trainer", "İptal")).Succeeded);
        Assert.Equal(0, await new SessionCompletionService(db).CompleteDueAsync(Now));
        Assert.Equal(8, member.RemainingClassCredits);
        Assert.False(lesson.CreditConsumed);
        Assert.Equal(PersonalTrainingSessionStatus.Cancelled, lesson.Status);
        Assert.Single(lesson.History);
    }

    [Theory]
    [InlineData(PersonalTrainingSessionStatus.Cancelled)]
    [InlineData(PersonalTrainingSessionStatus.NoShow)]
    [InlineData(PersonalTrainingSessionStatus.Postponed)]
    [InlineData(PersonalTrainingSessionStatus.Completed)]
    public async Task ExistingFinalOrPostponedLessons_AreNotReprocessed(PersonalTrainingSessionStatus status)
    {
        await using var db = Db();
        var lesson = Lesson(Member("existing"));
        lesson.Status = status;
        db.PersonalTrainingSessions.Add(lesson);
        await db.SaveChangesAsync();

        Assert.Equal(0, await new SessionCompletionService(db).CompleteDueAsync(Now));
        Assert.Equal(status, lesson.Status);
        Assert.Equal(8, lesson.MemberProfile.RemainingClassCredits);
        Assert.Empty(lesson.History);
    }

    [Fact]
    public async Task DueBoundary_CompletesOnlyEndedLessonsAndDebitsEachForTheSameMember()
    {
        await using var db = Db();
        var member = Member("boundary");
        var ended = Lesson(member);
        var exactEnd = Lesson(member);
        exactEnd.StartsAtUtc = Now.AddMinutes(-50);
        var ongoing = Lesson(member);
        ongoing.StartsAtUtc = Now.AddMinutes(-50).AddSeconds(1);
        var future = Lesson(member);
        future.StartsAtUtc = Now.AddMinutes(1);
        db.PersonalTrainingSessions.AddRange(ended, exactEnd, ongoing, future);
        await db.SaveChangesAsync();

        Assert.Equal(2, await new SessionCompletionService(db).CompleteDueAsync(Now));
        Assert.Equal(6, member.RemainingClassCredits);
        Assert.Equal(PersonalTrainingSessionStatus.Completed, exactEnd.Status);
        Assert.Equal(PersonalTrainingSessionStatus.Scheduled, ongoing.Status);
        Assert.Equal(PersonalTrainingSessionStatus.Scheduled, future.Status);
        Assert.Equal(2, await db.PersonalTrainingSessionHistories.CountAsync());
    }

    [Fact]
    public async Task ScheduledLessonWithAnExistingDebit_IsCompletedWithoutAnotherDebit()
    {
        await using var db = Db();
        var member = Member("already-paid");
        member.RemainingClassCredits = 7;
        var lesson = Lesson(member);
        lesson.CreditConsumed = true;
        db.PersonalTrainingSessions.Add(lesson);
        await db.SaveChangesAsync();

        Assert.Equal(1, await new SessionCompletionService(db).CompleteDueAsync(Now));
        Assert.Equal(7, member.RemainingClassCredits);
        Assert.True(lesson.CreditConsumed);
        Assert.Single(lesson.History);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task UnlimitedLegacyMembership_DoesNotDebitButASelectedPackageDoes(bool selectedPackage)
    {
        await using var db = Db();
        var member = Member("unlimited");
        member.MembershipPackage.WeeklyClassLimit = null;
        if (selectedPackage)
            member.ServicePackageVariant = new ServicePackageVariant
            {
                Name = "8 PT", PersonalTrainingSessionCount = 8,
                ServicePackage = new ServicePackage { Slug = "pt-test", Name = "PT", Category = ServicePackageCategory.PersonalTraining }
            };
        var lesson = Lesson(member);
        db.PersonalTrainingSessions.Add(lesson);
        await db.SaveChangesAsync();

        Assert.Equal(1, await new SessionCompletionService(db).CompleteDueAsync(Now));
        Assert.Equal(selectedPackage ? 7 : 8, member.RemainingClassCredits);
        Assert.True(lesson.CreditConsumed);
        Assert.Single(lesson.History);
    }

    [Fact]
    public async Task GroupCompletion_DoesNotDebitReservationCreditsAgain()
    {
        await using var db = Db();
        var member = Member("group");
        member.RemainingClassCredits = 7;
        var group = new ClassSession
        {
            StartsAtUtc = Now.AddHours(-2),
            GroupClass = new GroupClass { Name = "Reformer", DurationMinutes = 50, Capacity = 8,
                Trainer = new Trainer { FirstName = "Ece", LastName = "Test" } },
            Reservations = [new ClassReservation { MemberProfile = member }]
        };
        db.ClassSessions.Add(group);
        await db.SaveChangesAsync();

        Assert.Equal(1, await new SessionCompletionService(db).CompleteDueAsync(Now));
        Assert.Equal(ClassSessionStatus.Completed, group.Status);
        Assert.Equal(7, member.RemainingClassCredits);
        Assert.Equal(ClassReservationStatus.Reserved, Assert.Single(group.Reservations).Status);
        Assert.Empty(await db.PersonalTrainingSessionHistories.ToListAsync());
    }

    private static PersonalTrainingSession Lesson(MemberProfile member) => new()
    {
        MemberProfile = member, StartsAtUtc = Now.AddHours(-2), DurationMinutes = 50,
        Trainer = new Trainer { FirstName = "Deniz", LastName = "Test" }
    };

    private static MemberProfile Member(string name) => new()
    {
        ApplicationUser = new ApplicationUser { UserName = name }, ReferralCode = name,
        MembershipPackage = new MembershipPackage { Name = "Test", WeeklyClassLimit = 4 }, RemainingClassCredits = 8
    };

    private static ApplicationDbContext Db() => new(new DbContextOptionsBuilder<ApplicationDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
}
