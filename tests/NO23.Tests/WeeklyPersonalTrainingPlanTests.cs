using Microsoft.EntityFrameworkCore;
using NO23.Web.Domain.Entities;
using NO23.Web.Domain.Enums;
using NO23.Web.Services;
using NO23.Web.ViewModels.TrainerPanel;

namespace NO23.Tests;

public partial class PersonalTrainingCalendarServiceTests
{
    [Fact]
    public async Task Preview_EightTotalFourUsedThreeRemaining_ExposesMismatchWithoutRepairingTheBalance()
    {
        await using var db = CreateDbContext();
        var (trainer, member) = await SeedAsync(db);
        member.MembershipStartsAtUtc = ClubTime.ToUtc(new DateTime(2026, 10, 1));
        member.MembershipEndsAtUtc = ClubTime.ToUtc(new DateTime(2026, 11, 16));
        member.RemainingClassCredits = 3;
        member.ServicePackageVariant = new ServicePackageVariant { Name = "8 PT", PersonalTrainingSessionCount = 8,
            ServicePackage = new ServicePackage { Name = "PT", Slug = "8-pt" } };
        db.PersonalTrainingSessions.AddRange(Enumerable.Range(0, 4).Select(day => new PersonalTrainingSession
        {
            Trainer = trainer, MemberProfile = member, StartsAtUtc = member.MembershipStartsAtUtc.Value.AddDays(day + 1),
            Status = PersonalTrainingSessionStatus.Completed, CreditConsumed = true
        }));
        await db.SaveChangesAsync();
        var input = ProgramFor(member.Id, new DateTime(2026, 11, 30));
        var preview = await new PersonalTrainingCalendarService(db).PreviewWeeklyByAdminAsync(trainer.Id, input);
        Assert.Equal(8, preview.PackageLessons);
        Assert.Equal(4, preview.CompletedLessons);
        Assert.Equal(3, preview.RemainingCredits);
        Assert.NotNull(preview.CreditSummary);
        Assert.True(preview.CreditSummary.HasTotalDifference);
        Assert.True(preview.CreditSummary.NeedsReview);
        Assert.Equal(member.MembershipStartsAtUtc, preview.PackageStart);
        Assert.Contains("16.11.2026", preview.Message);
        Assert.Contains("30.11.2026", preview.Message);
        Assert.Equal(3, member.RemainingClassCredits);
        Assert.Equal(4, await db.PersonalTrainingSessions.CountAsync());
        input.Week = new DateTime(2026, 11, 2);
        var inPeriod = await new PersonalTrainingCalendarService(db).PreviewWeeklyByAdminAsync(trainer.Id, input);
        Assert.False(inPeriod.Succeeded);
        Assert.Equal(0, inPeriod.LessonCount);
        Assert.True(inPeriod.CreditSummary!.NeedsReview); // Changing the date must not conceal the separate balance discrepancy.
    }

    [Fact]
    public async Task Preview_MixedPackage_ExposesOverallCountsSeparatelyFromPersonalTraining()
    {
        await using var db = CreateDbContext();
        var (trainer, member) = await SeedAsync(db);
        member.MembershipStartsAtUtc = ClubTime.ToUtc(new DateTime(2026, 10, 1));
        member.MembershipEndsAtUtc = ClubTime.ToUtc(new DateTime(2026, 12, 1));
        member.RemainingClassCredits = 5;
        member.ServicePackageVariant = new ServicePackageVariant { Name = "Mixed", PersonalTrainingSessionCount = 8,
            GroupClassCreditCount = 2, ServicePackage = new ServicePackage { Name = "Mixed", Slug = "mixed" } };
        db.PersonalTrainingSessions.AddRange(Enumerable.Range(0, 4).Select(day => new PersonalTrainingSession
        {
            Trainer = trainer, MemberProfile = member, StartsAtUtc = member.MembershipStartsAtUtc.Value.AddDays(day + 1),
            Status = PersonalTrainingSessionStatus.Completed, CreditConsumed = true
        }));
        db.ClassSessions.Add(new ClassSession
        {
            StartsAtUtc = member.MembershipStartsAtUtc.Value.AddDays(5), Status = ClassSessionStatus.Completed,
            GroupClass = new GroupClass { Trainer = trainer, Name = "Group", DurationMinutes = 50, Capacity = 8 },
            Reservations = [new ClassReservation { MemberProfile = member }]
        });
        await db.SaveChangesAsync();
        var preview = await new PersonalTrainingCalendarService(db).PreviewWeeklyByAdminAsync(trainer.Id,
            ProgramFor(member.Id, new DateTime(2026, 11, 2)));
        Assert.Equal(8, preview.PackageLessons);
        Assert.Equal(4, preview.CompletedLessons);
        Assert.NotNull(preview.CreditSummary);
        Assert.Equal(10, preview.CreditSummary.PackageTotal);
        Assert.Equal(5, preview.CreditSummary.Completed);
        Assert.Equal(5, preview.RemainingCredits);
        Assert.False(preview.CreditSummary.NeedsReview);
    }

    [Fact]
    public async Task Preview_MonthlyLimitReached_ExplainsWhyRemainingCreditsCannotBeScheduled()
    {
        await using var db = CreateDbContext();
        var (trainer, member) = await SeedAsync(db);
        member.MembershipStartsAtUtc = ClubTime.ToUtc(new DateTime(2026, 10, 1));
        member.MembershipEndsAtUtc = ClubTime.ToUtc(new DateTime(2026, 12, 1));
        member.RemainingClassCredits = 2;
        member.ServicePackageVariant = new ServicePackageVariant { Name = "Monthly", PersonalTrainingSessionCount = 2,
            LessonsRenewMonthly = true, DurationMonths = 2, ServicePackage = new ServicePackage { Name = "PT", Slug = "monthly" } };
        db.PersonalTrainingSessions.AddRange(Enumerable.Range(0, 2).Select(day => new PersonalTrainingSession
        {
            Trainer = trainer, MemberProfile = member, StartsAtUtc = ClubTime.ToUtc(new DateTime(2026, 11, 1).AddDays(day)),
            Status = PersonalTrainingSessionStatus.Completed, CreditConsumed = true
        }));
        await db.SaveChangesAsync();
        var input = ProgramFor(member.Id, new DateTime(2026, 11, 9));
        var service = new PersonalTrainingCalendarService(db);
        var preview = await service.PreviewWeeklyByAdminAsync(trainer.Id, input);
        Assert.False(preview.Succeeded);
        Assert.Contains("aylık birebir ders sınırı (2)", preview.Message);
        Assert.Equal(0, preview.LessonCount);
        Assert.False((await service.CreateWeeklyByAdminAsync(trainer.Id, input)).Succeeded);
        Assert.Equal(2, member.RemainingClassCredits);
        Assert.Equal(2, await db.PersonalTrainingSessions.CountAsync());
    }

    [Fact]
    public async Task TwentyFourCredits_WithSeventeenDates_ExplainsThePeriodAndPreservesAllCredits()
    {
        await using var db = CreateDbContext();
        var (trainer, member) = await SeedAsync(db);
        member.MembershipStartsAtUtc = ClubTime.ToUtc(new DateTime(2026, 10, 1));
        member.MembershipEndsAtUtc = ClubTime.ToUtc(new DateTime(2026, 12, 1));
        member.RemainingClassCredits = 24;
        member.ServicePackageVariant = new ServicePackageVariant { Name = "24 PT", PersonalTrainingSessionCount = 24,
            ServicePackage = new ServicePackage { Name = "PT", Slug = "24-pt" } };
        await db.SaveChangesAsync();
        var input = ProgramFor(member.Id, new DateTime(2026, 9, 28));
        input.Days = [DayOfWeek.Tuesday, DayOfWeek.Thursday];
        var service = new PersonalTrainingCalendarService(db);

        var preview = await service.PreviewWeeklyByAdminAsync(trainer.Id, input);
        Assert.True(preview.Succeeded, preview.Message);
        Assert.Equal(17, preview.LessonCount);
        Assert.Contains("7 hak", preview.Message);
        Assert.Contains("01.10.2026", preview.Message);
        Assert.Contains("01.12.2026", preview.Message);
        Assert.True((await service.CreateWeeklyByAdminAsync(trainer.Id, input)).Succeeded);
        Assert.Equal(17, await db.PersonalTrainingSessions.CountAsync());
        Assert.Equal(24, member.RemainingClassCredits);
    }

    [Fact]
    public async Task Preview_AfterPackageEnd_ExplainsWhichDatesPreventScheduling()
    {
        await using var db = CreateDbContext();
        var (trainer, member) = await SeedAsync(db);
        member.MembershipStartsAtUtc = ClubTime.ToUtc(new DateTime(2026, 10, 1));
        member.MembershipEndsAtUtc = ClubTime.ToUtc(new DateTime(2026, 11, 30));
        member.RemainingClassCredits = 3;
        await db.SaveChangesAsync();
        var input = ProgramFor(member.Id, new DateTime(2026, 11, 30));
        input.Days = [DayOfWeek.Tuesday, DayOfWeek.Thursday];
        var service = new PersonalTrainingCalendarService(db);
        var preview = await service.PreviewWeeklyByAdminAsync(trainer.Id, input);
        Assert.False(preview.Succeeded);
        Assert.Contains("30.11.2026", preview.Message);
        Assert.Contains("paket", preview.Message.ToLowerInvariant());
        Assert.False((await service.CreateWeeklyByAdminAsync(trainer.Id, input)).Succeeded);
        Assert.Empty(db.PersonalTrainingSessions);
        Assert.Equal(3, member.RemainingClassCredits);
    }

    private static WeeklyPersonalSessionInput ProgramFor(int memberId, DateTime week) => new()
    {
        MemberProfileId = memberId, Week = week, Time = new TimeOnly(11, 0),
        Days = [DayOfWeek.Monday, DayOfWeek.Wednesday, DayOfWeek.Friday], DurationMinutes = 50
    };

    [Fact]
    public async Task SeventyLessons_PreviewAndSubmitMatchAndPackageTotalDoesNotChangeWithWeek()
    {
        await using var db = CreateDbContext();
        var (trainer, member) = await SeedAsync(db);
        var week = ClubTime.Monday(ClubTime.Now.AddDays(14));
        member.MembershipStartsAtUtc = ClubTime.ToUtc(week);
        member.MembershipEndsAtUtc = ClubTime.ToUtc(week.AddDays(175));
        member.RemainingClassCredits = 70;
        member.ServicePackageVariant = new ServicePackageVariant { Name = "70 PT", PersonalTrainingSessionCount = 70,
            ServicePackage = new ServicePackage { Name = "PT", Slug = "70-pt" } };
        await db.SaveChangesAsync();
        var service = new PersonalTrainingCalendarService(db);
        var input = ProgramFor(member.Id, week);

        var preview = await service.PreviewWeeklyByAdminAsync(trainer.Id, input);
        Assert.True(preview.Succeeded, preview.Message);
        Assert.Equal(70, preview.PackageLessons);
        Assert.Equal(70, preview.LessonCount);
        Assert.Equal(24, preview.WeekCount);
        input.Week = week.AddDays(7);
        var next = await service.PreviewWeeklyByAdminAsync(trainer.Id, input);
        Assert.Equal(70, next.PackageLessons);
        Assert.Equal(70, next.LessonCount);
        input.Week = week.AddDays(14);
        var shortWindow = await service.PreviewWeeklyByAdminAsync(trainer.Id, input);
        Assert.Equal(70, shortWindow.PackageLessons);
        Assert.Equal(69, shortWindow.LessonCount);
        Assert.Contains("1 hak", shortWindow.Message);
        input.Week = week;
        Assert.True((await service.CreateWeeklyByAdminAsync(trainer.Id, input)).Succeeded);
        Assert.Equal(preview.Starts, await db.PersonalTrainingSessions.OrderBy(s => s.StartsAtUtc).Select(s => s.StartsAtUtc).ToListAsync());
        Assert.Equal(70, member.RemainingClassCredits);
        var after = await service.PreviewWeeklyByAdminAsync(trainer.Id, input);
        Assert.Equal(70, after.PlannedLessons);
        Assert.Equal(0, after.AvailableLessons);
        Assert.False((await service.CreateWeeklyByAdminAsync(trainer.Id, input)).Succeeded);
        Assert.Equal(70, await db.PersonalTrainingSessions.CountAsync());
    }

    [Fact]
    public async Task Preview_SeparatesCompletedPendingAndUnchargedLessonsAcrossTheWholePackage()
    {
        await using var db = CreateDbContext();
        var (trainer, member) = await SeedAsync(db);
        var week = ClubTime.Monday(ClubTime.Now.AddDays(14));
        member.RemainingClassCredits = 9; // One of ten lessons has been charged.
        member.MembershipStartsAtUtc = ClubTime.ToUtc(week.AddDays(-21));
        member.MembershipEndsAtUtc = ClubTime.ToUtc(week.AddDays(35));
        db.PersonalTrainingSessions.AddRange(
            new() { TrainerId = trainer.Id, MemberProfile = member, StartsAtUtc = ClubTime.ToUtc(week.AddDays(-14)), Status = PersonalTrainingSessionStatus.Completed, CreditConsumed = true },
            new() { TrainerId = trainer.Id, MemberProfile = member, StartsAtUtc = ClubTime.ToUtc(week.AddDays(-7)), Status = PersonalTrainingSessionStatus.Completed },
            new() { TrainerId = trainer.Id, MemberProfile = member, StartsAtUtc = ClubTime.ToUtc(week.AddDays(-1)) },
            new() { TrainerId = trainer.Id, MemberProfile = member, StartsAtUtc = ClubTime.ToUtc(week.AddDays(7).AddHours(11)) },
            new() { TrainerId = trainer.Id, MemberProfile = member, StartsAtUtc = ClubTime.ToUtc(week.AddDays(2).AddHours(11)), Status = PersonalTrainingSessionStatus.Cancelled });
        await db.SaveChangesAsync();
        var preview = await new PersonalTrainingCalendarService(db).PreviewWeeklyByAdminAsync(trainer.Id, ProgramFor(member.Id, week));
        Assert.False(preview.Succeeded);
        Assert.Contains(CategoryCreditService.ReviewMessage, preview.Message);
        Assert.Equal(2, preview.CompletedLessons);
        Assert.Equal(2, preview.PlannedLessons);
        Assert.Equal(1, preview.UnchargedCompletedLessons);
        Assert.Null(preview.AvailableLessons);
        Assert.Empty(preview.Starts);
    }

    [Fact]
    public async Task MonthlyPackage_UsesEachMonthLimitAndTotalCredits()
    {
        await using var db = CreateDbContext();
        var (trainer, member) = await SeedAsync(db);
        var month = new DateTime(2027, 2, 1);
        member.MembershipStartsAtUtc = ClubTime.ToUtc(month);
        member.MembershipEndsAtUtc = ClubTime.ToUtc(month.AddMonths(2));
        member.RemainingClassCredits = 16;
        member.ServicePackageVariant = new ServicePackageVariant { Name = "Monthly PT", PersonalTrainingSessionCount = 8,
            LessonsRenewMonthly = true, DurationMonths = 2, ServicePackage = new ServicePackage { Name = "Monthly", Slug = "monthly-pt" } };
        await db.SaveChangesAsync();
        var service = new PersonalTrainingCalendarService(db);
        var input = ProgramFor(member.Id, month);
        var preview = await service.PreviewWeeklyByAdminAsync(trainer.Id, input);
        Assert.Equal(16, preview.PackageLessons);
        Assert.Equal(16, preview.LessonCount);
        Assert.All(preview.Starts.GroupBy(s => ClubTime.ToLocal(s).Month), g => Assert.Equal(8, g.Count()));
        Assert.True((await service.CreateWeeklyByAdminAsync(trainer.Id, input)).Succeeded);
    }

    [Fact]
    public async Task Preview_DoesNotAuthorizeAnUnassignedTrainer()
    {
        await using var db = CreateDbContext();
        var (trainer, member) = await SeedAsync(db);
        var other = new Trainer { FirstName = "Other" }; db.Add(other); await db.SaveChangesAsync();
        Assert.False((await new PersonalTrainingCalendarService(db).PreviewWeeklyAsync(other.Id,
            ProgramFor(member.Id, ClubTime.Monday(ClubTime.Now.AddDays(14))))).Succeeded);
    }

    [Theory]
    [InlineData(false, 1, 4, false)]
    [InlineData(true, 2, 4, false)]
    [InlineData(true, 1, 3, false)]
    [InlineData(true, 1, 4, true)]
    public async Task ReviewedCredits_RequireConfirmationAndMatchingCountAndBalance(bool confirmed, int count, int balance, bool success)
    {
        await using var db = CreateDbContext();
        var (trainer, member) = await SeedAsync(db);
        member.MembershipStartsAtUtc = DateTime.UtcNow.AddDays(-10);
        var legacy = new PersonalTrainingSession { TrainerId = trainer.Id, MemberProfile = member,
            StartsAtUtc = DateTime.UtcNow.AddDays(-2), Status = PersonalTrainingSessionStatus.Completed };
        db.Add(legacy);
        // Previous package and already charged lessons must never be charged by the review.
        db.Add(new PersonalTrainingSession { TrainerId = trainer.Id, MemberProfile = member,
            StartsAtUtc = DateTime.UtcNow.AddDays(-20), Status = PersonalTrainingSessionStatus.Completed });
        db.Add(new PersonalTrainingSession { TrainerId = trainer.Id, MemberProfile = member,
            StartsAtUtc = DateTime.UtcNow.AddDays(-3), Status = PersonalTrainingSessionStatus.Completed, CreditConsumed = true });
        await db.SaveChangesAsync();
        var service = new PersonalTrainingCalendarService(db);
        var result = await service.ApplyReviewedCreditsAsync(member.Id, count, balance, confirmed, "admin-review");
        Assert.Equal(success, result.Succeeded);
        Assert.Equal(success ? 3 : 4, member.RemainingClassCredits);
        Assert.Equal(success, legacy.CreditConsumed);
        if (!success) { Assert.Empty(legacy.History); return; }
        Assert.Equal("admin-review", Assert.Single(legacy.History).ChangedByUserId);
        Assert.False((await service.ApplyReviewedCreditsAsync(member.Id, 1, 3, true, "admin-review")).Succeeded);
        Assert.Equal(3, member.RemainingClassCredits);
        Assert.Single(legacy.History);
    }

    [Fact]
    public async Task PreviewIsNotTrusted_WhenBalanceChangesBeforeSubmit()
    {
        await using var db = CreateDbContext();
        var (trainer, member) = await SeedAsync(db);
        var week = ClubTime.Monday(ClubTime.Now.AddDays(14));
        member.MembershipEndsAtUtc = ClubTime.ToUtc(week.AddDays(28));
        await db.SaveChangesAsync();
        var service = new PersonalTrainingCalendarService(db);
        var input = ProgramFor(member.Id, week);
        Assert.Equal(4, (await service.PreviewWeeklyByAdminAsync(trainer.Id, input)).LessonCount);
        member.RemainingClassCredits = 2;
        await db.SaveChangesAsync();
        Assert.False((await service.CreateWeeklyByAdminAsync(trainer.Id, input)).Succeeded);
        Assert.Empty(db.PersonalTrainingSessions);
    }

    [Fact]
    public async Task PreviouslyAdjustedCredits_CanBeMarkedWithoutASecondDebit()
    {
        await using var db = CreateDbContext();
        var (trainer, member) = await SeedAsync(db);
        member.MembershipStartsAtUtc = DateTime.UtcNow.AddDays(-10);
        member.MembershipEndsAtUtc = DateTime.UtcNow.AddMonths(1);
        member.RemainingClassCredits = 3;
        var lesson = new PersonalTrainingSession { TrainerId = trainer.Id, MemberProfile = member,
            StartsAtUtc = DateTime.UtcNow.AddDays(-2), Status = PersonalTrainingSessionStatus.Completed };
        db.Add(lesson); await db.SaveChangesAsync();
        var service = new PersonalTrainingCalendarService(db);
        Assert.True((await service.ApplyReviewedCreditsAsync(member.Id, 1, 3, true, "admin", alreadyAdjusted: true)).Succeeded);
        Assert.Equal(3, member.RemainingClassCredits);
        Assert.True(lesson.CreditConsumed);
        Assert.Contains("bakiye korunarak", Assert.Single(lesson.History).Note);
        Assert.False((await service.ApplyReviewedCreditsAsync(member.Id, 1, 3, true, "admin", alreadyAdjusted: true)).Succeeded);
    }

    [Fact]
    public async Task ReviewedCredits_UnknownPackagePeriodDoesNotGuessHistoricalBalances()
    {
        await using var db = CreateDbContext();
        var (_, member) = await SeedAsync(db);
        member.MembershipStartsAtUtc = null;
        await db.SaveChangesAsync();
        Assert.False((await new PersonalTrainingCalendarService(db).ApplyReviewedCreditsAsync(member.Id, 1, 4, true, "admin")).Succeeded);
        Assert.Equal(4, member.RemainingClassCredits);
    }
}
