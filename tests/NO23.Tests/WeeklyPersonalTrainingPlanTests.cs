using Microsoft.EntityFrameworkCore;
using NO23.Web.Domain.Entities;
using NO23.Web.Domain.Enums;
using NO23.Web.Services;
using NO23.Web.ViewModels.TrainerPanel;

namespace NO23.Tests;

public partial class PersonalTrainingCalendarServiceTests
{
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
        Assert.True(preview.Succeeded, preview.Message);
        Assert.Equal(2, preview.CompletedLessons);
        Assert.Equal(2, preview.PlannedLessons);
        Assert.Equal(1, preview.UnchargedCompletedLessons);
        Assert.Equal(6, preview.AvailableLessons);
        Assert.Equal(6, preview.LessonCount);
        Assert.DoesNotContain(ClubTime.ToUtc(week.AddDays(7).AddHours(11)), preview.Starts);
        Assert.Contains(ClubTime.ToUtc(week.AddDays(2).AddHours(11)), preview.Starts);
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
        Assert.True((await service.CreateWeeklyByAdminAsync(trainer.Id, input)).Succeeded);
        Assert.Equal(2, await db.PersonalTrainingSessions.CountAsync());
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
        Assert.False((await new PersonalTrainingCalendarService(db).ApplyReviewedCreditsAsync(member.Id, 1, 4, true, "admin")).Succeeded);
        Assert.Equal(4, member.RemainingClassCredits);
    }
}
