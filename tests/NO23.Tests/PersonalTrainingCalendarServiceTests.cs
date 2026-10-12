using Microsoft.EntityFrameworkCore;
using NO23.Web.Data;
using NO23.Web.Domain.Entities;
using NO23.Web.Domain.Enums;
using NO23.Web.Services;
using NO23.Web.ViewModels.TrainerPanel;

namespace NO23.Tests;

public class PersonalTrainingCalendarServiceTests
{
    [Fact]
    public async Task CreateWeeklyAsync_CreatesFixedDaysWithinAvailableCredits()
    {
        await using var db = CreateDbContext();
        var (trainer, member) = await SeedAsync(db);
        var input = new WeeklyPersonalSessionInput
        {
            MemberProfileId = member.Id,
            Week = ClubTime.Monday(ClubTime.Now.AddDays(14)),
            Days = [DayOfWeek.Monday],
            Time = new TimeOnly(11, 0),
            Weeks = 4,
            DurationMinutes = 50
        };

        var result = await new PersonalTrainingCalendarService(db).CreateWeeklyAsync(trainer.Id, input);

        Assert.True(result.Succeeded);
        var sessions = await db.PersonalTrainingSessions.OrderBy(item => item.StartsAtUtc).ToListAsync();
        Assert.Equal(4, sessions.Count);
        Assert.All(sessions, item => Assert.Equal(50, item.DurationMinutes));
        Assert.Equal(7, (sessions[1].StartsAtUtc - sessions[0].StartsAtUtc).TotalDays);
        Assert.Equal(4, member.RemainingClassCredits);
    }

    [Fact]
    public async Task CreateWeeklyAsync_RejectsBatchExceedingAvailableCredits()
    {
        await using var db = CreateDbContext();
        var (trainer, member) = await SeedAsync(db);
        var input = new WeeklyPersonalSessionInput
        {
            MemberProfileId = member.Id,
            Week = ClubTime.Monday(ClubTime.Now.AddDays(14)),
            Days = [DayOfWeek.Monday],
            Time = new TimeOnly(11, 0),
            Weeks = 5
        };

        var result = await new PersonalTrainingCalendarService(db).CreateWeeklyAsync(trainer.Id, input);

        Assert.False(result.Succeeded);
        Assert.Empty(db.PersonalTrainingSessions);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(52)]
    public async Task CreateWeeklyAsync_KnownMembershipEndOverridesManualWeeksAndIncludesPartialFinalWeek(int weeks)
    {
        await using var db = CreateDbContext();
        var (trainer, member) = await SeedAsync(db);
        var week = ClubTime.Monday(ClubTime.Now.AddDays(14));
        member.RemainingClassCredits = 20;
        member.MembershipStartsAtUtc = DateTime.UtcNow.AddDays(-1);
        member.MembershipEndsAtUtc = ClubTime.ToUtc(week.AddDays(14).AddHours(12));
        await db.SaveChangesAsync();
        var input = new WeeklyPersonalSessionInput
        {
            MemberProfileId = member.Id,
            Week = week.AddDays(2),
            Days = [DayOfWeek.Friday, DayOfWeek.Monday, DayOfWeek.Wednesday, DayOfWeek.Monday],
            Time = new TimeOnly(11, 0),
            Weeks = weeks,
            DurationMinutes = 50
        };

        var result = await new PersonalTrainingCalendarService(db).CreateWeeklyAsync(trainer.Id, input);

        Assert.True(result.Succeeded, result.Message);
        var sessions = await db.PersonalTrainingSessions.OrderBy(item => item.StartsAtUtc).ToListAsync();
        var expected = new[] { 0, 2, 4, 7, 9, 11, 14 }
            .Select(day => ClubTime.ToUtc(week.AddDays(day).AddHours(11))).ToArray();
        Assert.Equal(expected, sessions.Select(item => item.StartsAtUtc).ToArray());
        Assert.All(sessions, item => Assert.Equal(50, item.DurationMinutes));
        Assert.Equal(20, member.RemainingClassCredits);
    }

    [Fact]
    public async Task CreateWeeklyAsync_KnownMembershipEndCanScheduleMoreThan52Weeks()
    {
        await using var db = CreateDbContext();
        var (trainer, member) = await SeedAsync(db);
        var week = ClubTime.Monday(ClubTime.Now.AddDays(14));
        member.RemainingClassCredits = 60;
        member.MembershipEndsAtUtc = ClubTime.ToUtc(week.AddDays(53 * 7));
        await db.SaveChangesAsync();
        var input = new WeeklyPersonalSessionInput
        {
            MemberProfileId = member.Id,
            Week = week,
            Days = [DayOfWeek.Monday],
            Time = new TimeOnly(11, 0),
            Weeks = 1,
            DurationMinutes = 50
        };

        var result = await new PersonalTrainingCalendarService(db).CreateWeeklyAsync(trainer.Id, input);

        Assert.True(result.Succeeded, result.Message);
        var starts = await db.PersonalTrainingSessions.OrderBy(item => item.StartsAtUtc)
            .Select(item => item.StartsAtUtc).ToListAsync();
        Assert.Equal(53, starts.Count);
        Assert.Equal(ClubTime.ToUtc(week.AddDays(52 * 7).AddHours(11)), starts[^1]);
        Assert.Equal(60, member.RemainingClassCredits);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(53)]
    public async Task CreateWeeklyAsync_UnknownMembershipEndKeepsManualWeekLimits(int weeks)
    {
        await using var db = CreateDbContext();
        var (trainer, member) = await SeedAsync(db);
        var input = new WeeklyPersonalSessionInput
        {
            MemberProfileId = member.Id,
            Week = ClubTime.Monday(ClubTime.Now.AddDays(14)),
            Days = [DayOfWeek.Monday],
            Time = new TimeOnly(11, 0),
            Weeks = weeks
        };

        var result = await new PersonalTrainingCalendarService(db).CreateWeeklyAsync(trainer.Id, input);

        Assert.False(result.Succeeded);
        Assert.Empty(db.PersonalTrainingSessions);
    }

    [Fact]
    public async Task CreateWeeklyAsync_KnownMembershipPeriodExcludesDatesBeforeMembershipStart()
    {
        await using var db = CreateDbContext();
        var (trainer, member) = await SeedAsync(db);
        var week = ClubTime.Monday(ClubTime.Now.AddDays(14));
        member.MembershipStartsAtUtc = ClubTime.ToUtc(week.AddHours(11).AddMinutes(1));
        member.MembershipEndsAtUtc = ClubTime.ToUtc(week.AddDays(4).AddHours(12));
        await db.SaveChangesAsync();
        var input = new WeeklyPersonalSessionInput
        {
            MemberProfileId = member.Id,
            Week = week,
            Days = [DayOfWeek.Monday, DayOfWeek.Wednesday, DayOfWeek.Friday],
            Time = new TimeOnly(11, 0),
            Weeks = 52,
            DurationMinutes = 50
        };

        var result = await new PersonalTrainingCalendarService(db).CreateWeeklyAsync(trainer.Id, input);

        Assert.True(result.Succeeded, result.Message);
        var starts = await db.PersonalTrainingSessions.OrderBy(item => item.StartsAtUtc)
            .Select(item => item.StartsAtUtc).ToListAsync();
        Assert.Equal(new[]
        {
            ClubTime.ToUtc(week.AddDays(2).AddHours(11)),
            ClubTime.ToUtc(week.AddDays(4).AddHours(11))
        }, starts.ToArray());
    }

    [Fact]
    public async Task CreateWeeklyAsync_StartWeekAfterMembershipEndRejectsWithoutBookings()
    {
        await using var db = CreateDbContext();
        var (trainer, member) = await SeedAsync(db);
        var week = ClubTime.Monday(ClubTime.Now.AddDays(14));
        member.MembershipEndsAtUtc = ClubTime.ToUtc(week.AddDays(7));
        await db.SaveChangesAsync();
        var input = new WeeklyPersonalSessionInput
        {
            MemberProfileId = member.Id,
            Week = week.AddDays(14),
            Days = [DayOfWeek.Monday],
            Time = new TimeOnly(11, 0),
            Weeks = 0
        };

        var result = await new PersonalTrainingCalendarService(db).CreateWeeklyAsync(trainer.Id, input);

        Assert.False(result.Succeeded);
        Assert.Empty(db.PersonalTrainingSessions);
        Assert.Equal(4, member.RemainingClassCredits);
    }

    [Theory]
    [InlineData(50, true)]
    [InlineData(49, false)]
    [InlineData(0, false)]
    public async Task CreateWeeklyAsync_MembershipEndRequiresTheCompleteLessonToFit(
        int minutesUntilEnd, bool expectedSuccess)
    {
        await using var db = CreateDbContext();
        var (trainer, member) = await SeedAsync(db);
        var week = ClubTime.Monday(ClubTime.Now.AddDays(14));
        var start = ClubTime.ToUtc(week.AddHours(11));
        member.MembershipEndsAtUtc = start.AddMinutes(minutesUntilEnd);
        await db.SaveChangesAsync();
        var input = new WeeklyPersonalSessionInput
        {
            MemberProfileId = member.Id,
            Week = week,
            Days = [DayOfWeek.Monday],
            Time = new TimeOnly(11, 0),
            Weeks = 0,
            DurationMinutes = 50
        };

        var result = await new PersonalTrainingCalendarService(db).CreateWeeklyAsync(trainer.Id, input);

        Assert.Equal(expectedSuccess, result.Succeeded);
        if (expectedSuccess)
        {
            var session = await db.PersonalTrainingSessions.SingleAsync();
            Assert.Equal(start, session.StartsAtUtc);
            Assert.Equal(member.MembershipEndsAtUtc, session.StartsAtUtc.AddMinutes(session.DurationMinutes));
        }
        else
        {
            Assert.Empty(db.PersonalTrainingSessions);
        }
        Assert.Equal(4, member.RemainingClassCredits);
    }

    [Fact]
    public async Task CreateWeeklyAsync_AutomaticProgramExceedingCreditsRejectsTheEntireBatch()
    {
        await using var db = CreateDbContext();
        var (trainer, member) = await SeedAsync(db);
        var week = ClubTime.Monday(ClubTime.Now.AddDays(14));
        member.MembershipEndsAtUtc = ClubTime.ToUtc(week.AddDays(35));
        await db.SaveChangesAsync();
        var input = new WeeklyPersonalSessionInput
        {
            MemberProfileId = member.Id,
            Week = week,
            Days = [DayOfWeek.Monday],
            Time = new TimeOnly(11, 0),
            Weeks = 1,
            DurationMinutes = 50
        };

        var result = await new PersonalTrainingCalendarService(db).CreateWeeklyAsync(trainer.Id, input);

        Assert.False(result.Succeeded);
        Assert.Empty(db.PersonalTrainingSessions);
        Assert.Equal(4, member.RemainingClassCredits);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CreateWeeklyAsync_AutomaticProgramStillEnforcesPackagePersonalTrainingLimits(
        bool lessonsRenewMonthly)
    {
        await using var db = CreateDbContext();
        var (trainer, member) = await SeedAsync(db);
        var nextMonth = ClubTime.Now.AddMonths(1);
        var week = ClubTime.Monday(new DateTime(nextMonth.Year, nextMonth.Month, 1).AddDays(6));
        member.RemainingClassCredits = 20;
        member.MembershipEndsAtUtc = ClubTime.ToUtc(week.AddDays(21));
        member.ServicePackageVariant = new ServicePackageVariant
        {
            Name = "PT", TotalPrice = 1000, PersonalTrainingSessionCount = 2,
            LessonsRenewMonthly = lessonsRenewMonthly,
            ServicePackage = new ServicePackage
            {
                Name = "PT", Slug = "pt-test", Category = ServicePackageCategory.PersonalTraining
            }
        };
        await db.SaveChangesAsync();
        var input = new WeeklyPersonalSessionInput
        {
            MemberProfileId = member.Id,
            Week = week,
            Days = [DayOfWeek.Monday, DayOfWeek.Wednesday],
            Time = new TimeOnly(11, 0),
            Weeks = 1,
            DurationMinutes = 50
        };

        var result = await new PersonalTrainingCalendarService(db).CreateWeeklyAsync(trainer.Id, input);

        Assert.False(result.Succeeded);
        Assert.Empty(db.PersonalTrainingSessions);
        Assert.Equal(20, member.RemainingClassCredits);
    }

    [Fact]
    public async Task CreateWeeklyAsync_ConflictLaterInAutomaticProgramRejectsTheEntireBatch()
    {
        await using var db = CreateDbContext();
        var (trainer, member) = await SeedAsync(db);
        var week = ClubTime.Monday(ClubTime.Now.AddDays(14));
        var conflictingStart = ClubTime.ToUtc(week.AddDays(14).AddHours(11));
        member.RemainingClassCredits = 20;
        member.MembershipEndsAtUtc = ClubTime.ToUtc(week.AddDays(21));
        db.PersonalTrainingSessions.Add(new PersonalTrainingSession
        {
            TrainerId = trainer.Id,
            MemberProfileId = member.Id,
            StartsAtUtc = conflictingStart,
            DurationMinutes = 50
        });
        await db.SaveChangesAsync();
        var input = new WeeklyPersonalSessionInput
        {
            MemberProfileId = member.Id,
            Week = week,
            Days = [DayOfWeek.Monday],
            Time = new TimeOnly(11, 0),
            Weeks = 1,
            DurationMinutes = 50
        };

        var result = await new PersonalTrainingCalendarService(db).CreateWeeklyAsync(trainer.Id, input);

        Assert.False(result.Succeeded);
        Assert.Equal(conflictingStart, (await db.PersonalTrainingSessions.SingleAsync()).StartsAtUtc);
        Assert.Equal(20, member.RemainingClassCredits);
    }

    [Fact]
    public async Task CreateWeeklyAsync_RejectsGroupOnlyPackage()
    {
        await using var db = CreateDbContext();
        var (trainer, member) = await SeedAsync(db);
        member.ServicePackageVariant = new ServicePackageVariant
        {
            Name = "Group only", TotalPrice = 1000,
            ServicePackage = new ServicePackage { Name = "Group", Slug = "group-test", Category = ServicePackageCategory.GroupClasses }
        };
        await db.SaveChangesAsync();
        var input = new WeeklyPersonalSessionInput
        {
            MemberProfileId = member.Id,
            Week = ClubTime.Monday(ClubTime.Now.AddDays(14)),
            Days = [DayOfWeek.Monday], Weeks = 1
        };

        var result = await new PersonalTrainingCalendarService(db).CreateWeeklyAsync(trainer.Id, input);

        Assert.False(result.Succeeded);
        Assert.Empty(db.PersonalTrainingSessions);
    }

    [Fact]
    public async Task CreateWeeklyByAdminAsync_AllowsCurrentAndPastWeeks()
    {
        await using var db = CreateDbContext();
        var (trainer, member) = await SeedAsync(db);
        var input = new WeeklyPersonalSessionInput
        {
            MemberProfileId = member.Id,
            Week = ClubTime.Monday(ClubTime.Now.AddDays(-7)),
            Days = [DayOfWeek.Monday],
            Time = new TimeOnly(11, 0),
            Weeks = 2,
            DurationMinutes = 50
        };

        var result = await new PersonalTrainingCalendarService(db)
            .CreateWeeklyByAdminAsync(trainer.Id, input);

        Assert.True(result.Succeeded);
        var sessions = await db.PersonalTrainingSessions
            .OrderBy(item => item.StartsAtUtc)
            .ToListAsync();
        Assert.Equal(2, sessions.Count);
        Assert.Contains(sessions, item => item.StartsAtUtc <= DateTime.UtcNow);
    }

    [Fact]
    public async Task CreateWeeklyAsync_StillRejectsPastDatesForTrainer()
    {
        await using var db = CreateDbContext();
        var (trainer, member) = await SeedAsync(db);
        var input = new WeeklyPersonalSessionInput
        {
            MemberProfileId = member.Id,
            Week = ClubTime.Monday(ClubTime.Now.AddDays(-7)),
            Days = [DayOfWeek.Monday],
            Time = new TimeOnly(11, 0),
            Weeks = 1
        };

        var result = await new PersonalTrainingCalendarService(db)
            .CreateWeeklyAsync(trainer.Id, input);

        Assert.False(result.Succeeded);
        Assert.Empty(db.PersonalTrainingSessions);
    }

    [Fact]
    public async Task CreateAsync_RejectsMemberAssignedToAnotherTrainer()
    {
        await using var db = CreateDbContext();
        var (trainer, member) = await SeedAsync(db);
        var otherTrainer = new Trainer { FirstName = "Other", LastName = "Trainer", Specialty = "PT" };
        db.Trainers.Add(otherTrainer);
        await db.SaveChangesAsync();

        var result = await new PersonalTrainingCalendarService(db).CreateAsync(
            otherTrainer.Id, member.Id, DateTime.UtcNow.AddDays(1), 60, null);

        Assert.False(result.Succeeded);
        Assert.Empty(db.PersonalTrainingSessions);
    }

    [Fact]
    public async Task CreateByAdminAsync_AllowsDifferentTrainerWithoutChangingMemberAssignment()
    {
        await using var db = CreateDbContext();
        var (assignedTrainer, member) = await SeedAsync(db);
        var otherTrainer = new Trainer { FirstName = "Other", LastName = "Trainer", Specialty = "PT" };
        db.Trainers.Add(otherTrainer);
        await db.SaveChangesAsync();

        var result = await new PersonalTrainingCalendarService(db).CreateByAdminAsync(
            otherTrainer.Id, member.Id, DateTime.UtcNow.AddDays(1), 50, null);

        Assert.True(result.Succeeded);
        Assert.Equal(otherTrainer.Id, (await db.PersonalTrainingSessions.SingleAsync()).TrainerId);
        Assert.Equal(assignedTrainer.Id, member.AssignedTrainerId);
    }

    [Fact]
    public async Task CreateByAdminAsync_AllowsPastManualSession()
    {
        await using var db = CreateDbContext();
        var (trainer, member) = await SeedAsync(db);
        var startsAtUtc = DateTime.UtcNow.AddDays(-1);

        var result = await new PersonalTrainingCalendarService(db)
            .CreateByAdminAsync(trainer.Id, member.Id, startsAtUtc, 50, "Geçmiş ders kaydı");

        Assert.True(result.Succeeded);
        Assert.Equal(startsAtUtc, (await db.PersonalTrainingSessions.SingleAsync()).StartsAtUtc);
    }

    [Fact]
    public async Task CancelledSession_PreservesCreditAndIsNotProcessedTwice()
    {
        await using var db = CreateDbContext();
        var (trainer, member) = await SeedAsync(db);
        var service = new PersonalTrainingCalendarService(db);
        await service.CreateAsync(trainer.Id, member.Id, DateTime.UtcNow.AddDays(1), 60, null);
        var session = await db.PersonalTrainingSessions.SingleAsync();

        var result = await service.ChangeStatusAsync(trainer.Id, session.Id,
            PersonalTrainingSessionStatus.Cancelled, null, "trainer-user", "Müşteri iptal etti");
        var secondResult = await service.ChangeStatusAsync(trainer.Id, session.Id,
            PersonalTrainingSessionStatus.Cancelled, null, "trainer-user", null);

        Assert.True(result.Succeeded);
        Assert.False(secondResult.Succeeded);
        Assert.Equal(4, member.RemainingClassCredits);
        Assert.False(session.CreditConsumed);
        Assert.Single(db.PersonalTrainingSessionHistories);
    }

    [Fact]
    public async Task PostponedSession_KeepsCreditAndUpdatesDateWithHistory()
    {
        await using var db = CreateDbContext();
        var (trainer, member) = await SeedAsync(db);
        var service = new PersonalTrainingCalendarService(db);
        var original = DateTime.UtcNow.AddDays(1);
        var postponed = original.AddDays(2);
        await service.CreateAsync(trainer.Id, member.Id, original, 60, "İlk ders detayı");
        var session = await db.PersonalTrainingSessions.SingleAsync();

        var result = await service.ChangeStatusAsync(trainer.Id, session.Id,
            PersonalTrainingSessionStatus.Postponed, postponed, "trainer-user", "Üye erteledi");

        Assert.True(result.Succeeded);
        Assert.Equal(4, member.RemainingClassCredits);
        Assert.False(session.CreditConsumed);
        Assert.Equal(PersonalTrainingSessionStatus.Scheduled, session.Status);
        Assert.Equal(postponed, session.StartsAtUtc);
        Assert.Equal("İlk ders detayı", session.Note);
        Assert.Equal(PersonalTrainingSessionStatus.Postponed,
            (await db.PersonalTrainingSessionHistories.SingleAsync()).NewStatus);
        Assert.Equal("Üye erteledi",
            (await db.PersonalTrainingSessionHistories.SingleAsync()).Note);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(50)]
    [InlineData(60)]
    public async Task PostponedSession_RejectsCrossingOrPassingMembershipEndWithoutMutation(int postponedMinuteOffset)
    {
        await using var db = CreateDbContext();
        var (trainer, member) = await SeedAsync(db);
        var original = ClubTime.ToUtc(ClubTime.Monday(ClubTime.Now.AddDays(14)).AddHours(11));
        var lastEligibleStart = original.AddDays(2);
        member.MembershipEndsAtUtc = lastEligibleStart.AddMinutes(50);
        await db.SaveChangesAsync();
        var service = new PersonalTrainingCalendarService(db);
        var created = await service.CreateAsync(trainer.Id, member.Id, original, 50, "İlk ders detayı");
        Assert.True(created.Succeeded, created.Message);
        var session = await db.PersonalTrainingSessions.SingleAsync();
        var originalUpdatedAt = session.UpdatedAtUtc;

        var result = await service.ChangeStatusAsync(trainer.Id, session.Id,
            PersonalTrainingSessionStatus.Postponed, lastEligibleStart.AddMinutes(postponedMinuteOffset),
            "trainer-user", "Üye erteledi");

        Assert.False(result.Succeeded);
        var persisted = await db.PersonalTrainingSessions.AsNoTracking().SingleAsync();
        Assert.Equal(original, persisted.StartsAtUtc);
        Assert.Equal(PersonalTrainingSessionStatus.Scheduled, persisted.Status);
        Assert.Equal("İlk ders detayı", persisted.Note);
        Assert.Equal(originalUpdatedAt, persisted.UpdatedAtUtc);
        Assert.False(persisted.CreditConsumed);
        Assert.Equal(4, member.RemainingClassCredits);
        Assert.Empty(db.PersonalTrainingSessionHistories);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CancelledLesson_DoesNotUsePackageSessionAllowance(bool renewsMonthly)
    {
        await using var db = CreateDbContext();
        var (trainer, member) = await SeedAsync(db);
        var start = ClubTime.ToUtc(ClubTime.Monday(ClubTime.Now.AddDays(14)).AddHours(11));
        member.ServicePackageVariant = new ServicePackageVariant {
            Name = "PT", TotalPrice = 1000, PersonalTrainingSessionCount = 1,
            LessonsRenewMonthly = renewsMonthly,
            ServicePackage = new ServicePackage { Name = "PT", Slug = "pt-test", Category = ServicePackageCategory.PersonalTraining }
        };
        await db.SaveChangesAsync();
        var service = new PersonalTrainingCalendarService(db);
        Assert.True((await service.CreateAsync(trainer.Id, member.Id, start, 50, null)).Succeeded);
        var original = await db.PersonalTrainingSessions.SingleAsync();
        Assert.True((await service.ChangeStatusAsync(trainer.Id, original.Id,
            PersonalTrainingSessionStatus.Cancelled, null, "trainer-user", null)).Succeeded);

        var replacement = await service.CreateAsync(trainer.Id, member.Id, start, 50, null);

        Assert.True(replacement.Succeeded, replacement.Message);
        Assert.Equal(4, member.RemainingClassCredits);
        Assert.Equal(2, await db.PersonalTrainingSessions.CountAsync());
    }

    [Fact]
    public async Task InactiveLegacyGroup_DoesNotBlockPersonalLessonSlot()
    {
        await using var db = CreateDbContext();
        var (trainer, member) = await SeedAsync(db);
        var start = DateTime.UtcNow.AddDays(2);
        db.ClassSessions.Add(new ClassSession { StartsAtUtc = start, GroupClass = new GroupClass {
            Name = "Inactive", Trainer = trainer, Capacity = 8, DurationMinutes = 60, IsActive = false
        }});
        await db.SaveChangesAsync();
        var result = await new PersonalTrainingCalendarService(db).CreateAsync(trainer.Id, member.Id, start, 50, null);
        Assert.True(result.Succeeded, result.Message);
    }

    private static ApplicationDbContext CreateDbContext() => new(
        new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private static async Task<(Trainer Trainer, MemberProfile Member)> SeedAsync(ApplicationDbContext db)
    {
        var trainer = new Trainer { FirstName = "Test", LastName = "Trainer", Specialty = "PT" };
        var member = new MemberProfile
        {
            ApplicationUser = new ApplicationUser { UserName = "member@test.local", Email = "member@test.local" },
            MembershipPackage = new MembershipPackage
            {
                Code = MembershipPackageCode.Pro, Name = "Pro", Audience = "Test", Description = "Test",
                WeeklyClassLimit = 4
            },
            AssignedTrainer = trainer,
            RemainingClassCredits = 4
        };
        db.Add(member);
        await db.SaveChangesAsync();
        return (trainer, member);
    }
}
