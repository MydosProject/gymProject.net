using Microsoft.EntityFrameworkCore;
using NO23.Web.Data;
using NO23.Web.Domain.Entities;
using NO23.Web.Domain.Enums;
using NO23.Web.Services;

namespace NO23.Tests;

public class LessonCreditSummaryTests
{
    [Fact]
    public async Task MixedLessons_KeepPurchasedRemainingPlannedAndAvailableSeparateAcrossTheirLifecycle()
    {
        await using var db = Db();
        var member = Member(db, 10);
        var coach = new Trainer { FirstName = "QA" };
        db.Trainers.Add(coach);
        var completed = Personal(member, coach, PersonalTrainingSessionStatus.Completed, true);
        var pending = Personal(member, coach);
        db.PersonalTrainingSessions.AddRange(completed, pending);
        var oldGroup = Group(coach, ClassSessionStatus.Completed);
        var nextGroup = Group(coach);
        oldGroup.Reservations.Add(new ClassReservation { MemberProfile = member });
        nextGroup.Reservations.Add(new ClassReservation { MemberProfile = member });
        db.ClassSessions.AddRange(oldGroup, nextGroup);
        member.RemainingClassCredits = 7; // One PT used, one group used, one group reserved.
        await db.SaveChangesAsync();
        var service = new LessonCreditSummaryService(db);
        var summary = (await service.GetAsync(member.Id))!;
        Assert.Equal(10, summary.PackageTotal);
        Assert.Equal(2, summary.Completed);
        Assert.Equal(8, summary.Remaining);
        Assert.Equal(2, summary.Planned);
        Assert.Equal(6, summary.Available);
        Assert.False(summary.NeedsReview);
        var preview = await new PersonalTrainingCalendarService(db).PreviewWeeklyByAdminAsync(coach.Id,
            new NO23.Web.ViewModels.TrainerPanel.WeeklyPersonalSessionInput { MemberProfileId = member.Id, Week = ClubTime.Now.AddDays(7), Days = [DayOfWeek.Monday] });
        Assert.Equal(summary.Remaining, preview.RemainingCredits);
        Assert.Equal(7, preview.StoredBalance);
        var goals = new NO23.Web.Areas.Member.Controllers.GoalsController(db, new ClubClock(TimeProvider.System))
        {
            ControllerContext = new Microsoft.AspNetCore.Mvc.ControllerContext
            {
                HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
                {
                    User = new System.Security.Claims.ClaimsPrincipal(new System.Security.Claims.ClaimsIdentity(
                        [new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, member.ApplicationUserId)], "test"))
                }
            }
        };
        var goalModel = Assert.IsType<NO23.Web.ViewModels.Member.MemberGoalsIndexViewModel>(Assert.IsType<Microsoft.AspNetCore.Mvc.ViewResult>(await goals.Index()).Model);
        Assert.Equal(summary.Remaining, goalModel.RemainingClassCredits);
        goals.ModelState.AddModelError("FitnessGoal", "invalid");
        var invalidGoals = Assert.IsType<NO23.Web.ViewModels.Member.MemberGoalsIndexViewModel>(Assert.IsType<Microsoft.AspNetCore.Mvc.ViewResult>(await goals.Index(new())).Model);
        Assert.Equal(summary.Remaining, invalidGoals.RemainingClassCredits);

        var anotherGroup = Group(coach);
        db.ClassSessions.Add(anotherGroup); await db.SaveChangesAsync();
        var reservations = new ClassReservationService(db);
        Assert.True((await reservations.ReserveAsync(member.ApplicationUserId, anotherGroup.Id)).Succeeded);
        summary = (await service.GetAsync(member.Id))!;
        Assert.Equal(8, summary.Remaining); Assert.Equal(3, summary.Planned); Assert.Equal(5, summary.Available);
        Assert.True((await new PersonalTrainingCalendarService(db).ChangeStatusAsync(coach.Id, pending.Id,
            PersonalTrainingSessionStatus.Completed, null, "admin", null)).Succeeded);
        summary = (await service.GetAsync(member.Id))!;
        Assert.Equal(7, summary.Remaining); Assert.Equal(3, summary.Completed); Assert.Equal(5, summary.Available);
        var booking = await db.ClassReservations.SingleAsync(x => x.ClassSessionId == anotherGroup.Id);
        Assert.True((await reservations.CancelByAdminAsync(anotherGroup.Id, booking.Id)).Succeeded);
        Assert.False((await reservations.CancelByAdminAsync(anotherGroup.Id, booking.Id)).Succeeded);
        summary = (await service.GetAsync(member.Id))!;
        Assert.Equal(7, summary.Remaining); Assert.Equal(1, summary.Planned); Assert.Equal(6, summary.Available);
        nextGroup.StartsAtUtc = DateTime.UtcNow.AddHours(-2); oldGroup.StartsAtUtc = nextGroup.StartsAtUtc.AddHours(-1);
        await db.SaveChangesAsync();
        await new SessionCompletionService(db).CompleteDueAsync(DateTime.UtcNow);
        summary = (await service.GetAsync(member.Id))!;
        Assert.Equal(6, summary.Remaining); Assert.Equal(4, summary.Completed); Assert.Equal(0, summary.Planned);
        Assert.Equal(6, summary.Available); Assert.False(summary.NeedsReview);
    }

    [Fact]
    public async Task GroupBooking_CannotSpendTheLastCreditAlreadyReservedForPersonalTraining()
    {
        await using var db = Db();
        var member = Member(db, 1);
        var coach = new Trainer();
        var personal = Personal(member, coach);
        var group = Group(coach);
        db.PersonalTrainingSessions.Add(personal); db.ClassSessions.Add(group); await db.SaveChangesAsync();
        Assert.False((await new ClassReservationService(db).ReserveAsync(member.ApplicationUserId, group.Id)).Succeeded);
        Assert.Equal(1, member.RemainingClassCredits);
        Assert.Empty(db.ClassReservations);
        Assert.True((await new PersonalTrainingCalendarService(db).ChangeStatusAsync(coach.Id, personal.Id,
            PersonalTrainingSessionStatus.Cancelled, null, "admin", null)).Succeeded);
        Assert.True((await new ClassReservationService(db).ReserveAsync(member.ApplicationUserId, group.Id)).Succeeded);
        Assert.Equal(0, member.RemainingClassCredits);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task LegacyCompletedAndNoShow_AreReviewedOnceWithoutGuessingPreviousManualDebits(bool alreadyAdjusted)
    {
        await using var db = Db();
        var member = Member(db, 10);
        var coach = new Trainer();
        db.PersonalTrainingSessions.AddRange(Personal(member, coach, PersonalTrainingSessionStatus.Completed),
            Personal(member, coach, PersonalTrainingSessionStatus.NoShow));
        member.RemainingClassCredits = alreadyAdjusted ? 8 : 10;
        await db.SaveChangesAsync();
        var service = new LessonCreditSummaryService(db);
        var summary = (await service.GetAsync(member.Id))!;
        Assert.Equal(2, summary.UnreviewedCompleted); Assert.True(summary.NeedsReview);
        Assert.Equal(alreadyAdjusted ? 8 : 10, member.RemainingClassCredits); // Reading never repairs data.
        var review = new PersonalTrainingCalendarService(db);
        Assert.False((await review.ApplyReviewedCreditsAsync(member.Id, 2, 99, true, "admin", alreadyAdjusted)).Succeeded);
        Assert.True((await review.ApplyReviewedCreditsAsync(member.Id, 2, member.RemainingClassCredits, true, "admin", alreadyAdjusted)).Succeeded);
        Assert.Equal(8, member.RemainingClassCredits);
        Assert.False((await review.ApplyReviewedCreditsAsync(member.Id, 2, 8, true, "admin", alreadyAdjusted)).Succeeded);
        summary = (await service.GetAsync(member.Id))!;
        Assert.False(summary.NeedsReview); Assert.Equal(8, summary.Remaining); Assert.Equal(2, summary.Completed);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task LegacyCancelledGroup_RefundReviewIsExplicitAndCannotRefundTwice(bool alreadyAdjusted)
    {
        await using var db = Db();
        var member = Member(db, 10);
        member.RemainingClassCredits = alreadyAdjusted ? 10 : 9;
        var group = Group(new Trainer(), ClassSessionStatus.Cancelled);
        group.Reservations.Add(new ClassReservation { MemberProfile = member });
        db.ClassSessions.Add(group); await db.SaveChangesAsync();
        var summary = (await new LessonCreditSummaryService(db).GetAsync(member.Id))!;
        Assert.Equal(1, summary.InvalidGroupReservations);
        var review = new PersonalTrainingCalendarService(db);
        Assert.False((await review.ReviewCancelledGroupCreditsAsync(member.Id, 1, member.RemainingClassCredits, false, "admin", alreadyAdjusted)).Succeeded);
        Assert.True((await review.ReviewCancelledGroupCreditsAsync(member.Id, 1, member.RemainingClassCredits, true, new string('x',450), alreadyAdjusted)).Succeeded);
        Assert.Equal(10, member.RemainingClassCredits);
        Assert.True(group.Reservations.Single().CancellationReason!.Length <= 300);
        Assert.False((await review.ReviewCancelledGroupCreditsAsync(member.Id, 1, 10, true, "admin", alreadyAdjusted)).Succeeded);
        Assert.False((await new LessonCreditSummaryService(db).GetAsync(member.Id))!.NeedsReview);
    }

    [Fact]
    public async Task PeriodScopes_AllPanelsReadTheSameBalanceAndExpiredCreditsDoNotDisappear()
    {
        await using var db = Db();
        var member = Member(db, 10);
        var coach = new Trainer();
        var beforePeriod = Personal(member, coach, PersonalTrainingSessionStatus.Completed, true);
        beforePeriod.StartsAtUtc = member.MembershipStartsAtUtc!.Value.AddDays(-1);
        db.PersonalTrainingSessions.Add(beforePeriod); await db.SaveChangesAsync();
        var service = new LessonCreditSummaryService(db);
        Assert.Equal(0, (await service.GetAsync(member.Id))!.Completed);
        member.MembershipEndsAtUtc = DateTime.UtcNow.AddMinutes(-1); await db.SaveChangesAsync();
        var summary = (await service.GetAsync(member.Id))!;
        Assert.False(summary.IsPackageActive); Assert.Equal(10, summary.Remaining);
        Assert.Equal(10, (await service.LoadAsync([member.Id]))[member.Id].Remaining);
        var future = Group(coach); db.ClassSessions.Add(future); await db.SaveChangesAsync();
        Assert.False((await new ClassReservationService(db).ReserveAsync(member.ApplicationUserId, future.Id)).Succeeded);
        Assert.Equal(10, member.RemainingClassCredits);
    }

    [Theory]
    [InlineData(MembershipPackageCode.KitchenOnly, false)]
    [InlineData(MembershipPackageCode.Elite, true)]
    public async Task KitchenAndUnlimitedProfiles_AreNotMisreportedAsFiniteSportsBalances(MembershipPackageCode code, bool unlimited)
    {
        await using var db = Db();
        var member = Member(db, 0); member.ServicePackageVariant = null; member.ServicePackageVariantId = null;
        member.MembershipPackage.Code = code; member.MembershipPackage.WeeklyClassLimit = null;
        await db.SaveChangesAsync();
        var summary = (await new LessonCreditSummaryService(db).GetAsync(member.Id))!;
        Assert.Equal(unlimited, summary.IsUnlimited); Assert.Equal(unlimited, summary.HasSportsPackage);
    }

    [Fact]
    public async Task OptimisticBalanceCheck_DetectsAConcurrentDebitRatherThanOverwritingIt()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        await using var db1 = new ApplicationDbContext(options);
        var member = Member(db1, 10); await db1.SaveChangesAsync();
        await using var db2 = new ApplicationDbContext(options);
        var other = await db2.MemberProfiles.SingleAsync();
        member.RemainingClassCredits--; await db1.SaveChangesAsync();
        other.RemainingClassCredits--;
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => db2.SaveChangesAsync());
    }

    private static ApplicationDbContext Db() => new(new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
    private static MemberProfile Member(ApplicationDbContext db, int credits)
    {
        var member = new MemberProfile { ApplicationUser = new ApplicationUser { UserName = Guid.NewGuid().ToString() },
            MembershipPackage = new MembershipPackage { Code = MembershipPackageCode.Pro, WeeklyClassLimit = 4 },
            ServicePackageVariant = new ServicePackageVariant { ServicePackage = new ServicePackage { Name = "Test" }, PersonalTrainingSessionCount = credits, IsActive = true },
            RemainingClassCredits = credits, MembershipStartsAtUtc = DateTime.UtcNow.AddDays(-30), MembershipEndsAtUtc = DateTime.UtcNow.AddDays(30) };
        db.MemberProfiles.Add(member); return member;
    }
    private static PersonalTrainingSession Personal(MemberProfile member, Trainer trainer,
        PersonalTrainingSessionStatus status = PersonalTrainingSessionStatus.Scheduled, bool consumed = false) =>
        new() { MemberProfile = member, Trainer = trainer, StartsAtUtc = DateTime.UtcNow.AddDays(1), DurationMinutes = 50, Status = status, CreditConsumed = consumed };
    private static ClassSession Group(Trainer trainer, ClassSessionStatus status = ClassSessionStatus.Scheduled) =>
        new() { GroupClass = new GroupClass { Trainer = trainer, Name = "Grup", Capacity = 10 }, StartsAtUtc = DateTime.UtcNow.AddDays(2), Status = status };
}
