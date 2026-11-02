using Microsoft.EntityFrameworkCore;
using NO23.Web.Data;
using NO23.Web.Domain.Entities;
using NO23.Web.Domain.Enums;
using NO23.Web.Services;

namespace NO23.Tests;

public class MemberPersonalScheduleTests
{
    [Theory]
    [InlineData(4, 8, false, true)]
    [InlineData(0, 8, true, false)]
    public async Task SelectedVariant_DeterminesPtAccessRatherThanLegacyPackageFlag(int pt, int group, bool legacyFlag, bool allowed)
    {
        await using var db = Db();
        var member = Seed(db, pt, group);
        member.MembershipPackage.IncludesPersonalTrainingSupport = legacyFlag;
        await db.SaveChangesAsync();
        Assert.Equal(allowed, MemberPackageEntitlement.IncludesPersonalTraining(member));
        var summary = (await new LessonCreditSummaryService(db).GetAsync(member.Id))!;
        Assert.Equal(pt, summary.PersonalTrainingTotal);
        Assert.Equal(group, summary.GroupTotal);
        Assert.Equal(pt + group, summary.PackageTotal);
        var trainer = new Trainer { FirstName = "QA", LastName = "PT" };
        db.Add(trainer); await db.SaveChangesAsync();
        var requestService = new PersonalTrainingRequestService(db, new ClubClock(TimeProvider.System));
        var result = await requestService.CreateAsync(member.ApplicationUserId, new NO23.Web.ViewModels.Member.PersonalTrainingRequestInputViewModel {
            TrainerId = trainer.Id, PreferredDate = DateOnly.FromDateTime(ClubTime.Today.AddDays(1)), PreferredTimeWindow = PersonalTrainingRequestService.PreferredTimeWindows[0]
        });
        Assert.Equal(allowed, result.Succeeded);
        Assert.Equal(allowed ? 1 : 0, await db.PersonalTrainingRequests.CountAsync());
    }

    [Fact]
    public async Task ScheduledPt_IsVisibleOnlyToItsMember_ExcludesCancelledPastAndOutOfPeriod()
    {
        await using var db = Db();
        var member = Seed(db, 8, 8);
        var other = Seed(db, 8, 8);
        var trainer = new Trainer { FirstName = "QA", LastName = "Coach" };
        var now = DateTime.UtcNow;
        var visible = new PersonalTrainingSession { MemberProfile = member, Trainer = trainer, StartsAtUtc = now.AddDays(1), DurationMinutes = 50 };
        db.AddRange(visible,
            new PersonalTrainingSession { MemberProfile = other, Trainer = trainer, StartsAtUtc = now.AddDays(2) },
            new PersonalTrainingSession { MemberProfile = member, Trainer = trainer, StartsAtUtc = now.AddDays(3), Status = PersonalTrainingSessionStatus.Cancelled },
            new PersonalTrainingSession { MemberProfile = member, Trainer = trainer, StartsAtUtc = now.AddDays(-1) },
            new PersonalTrainingSession { MemberProfile = member, Trainer = trainer, StartsAtUtc = member.MembershipEndsAtUtc!.Value.AddDays(1) });
        await db.SaveChangesAsync();
        var sessions = await new MemberPersonalScheduleService(db).UpcomingAsync(member.Id, now);
        var session = Assert.Single(sessions);
        Assert.Equal(visible.Id, session.Id);
        Assert.Equal("QA Coach", session.TrainerName);
        Assert.Equal(50, session.DurationMinutes);
        var controller = new NO23.Web.Areas.Member.Controllers.HomeController(db, new ClassReservationService(db)) {
            ControllerContext = new Microsoft.AspNetCore.Mvc.ControllerContext { HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext {
                User = new System.Security.Claims.ClaimsPrincipal(new System.Security.Claims.ClaimsIdentity(
                    [new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, member.ApplicationUserId)], "test"))
            } }
        };
        var model = Assert.IsType<NO23.Web.ViewModels.Member.MemberDashboardViewModel>(Assert.IsType<Microsoft.AspNetCore.Mvc.ViewResult>(await controller.Index()).Model);
        Assert.Equal(visible.Id, Assert.Single(model.UpcomingPersonalSessions).Id);
    }

    [Fact]
    public async Task LegacyOption_KeepsItsRecordedPtAndGroupDefinitionWithoutLookingUpTodaysCatalog()
    {
        await using var db = Db();
        var member = Seed(db, 0, 0);
        member.ServicePackageVariant = null;
        member.MembershipPackageOption = new MembershipPackageOption { MembershipPackage = member.MembershipPackage,
            Name = "Recorded option", PersonalTrainingSessionCount = 8, GroupClassCreditCount = 4, DurationDays = 60 };
        await db.SaveChangesAsync();
        var summary = (await new LessonCreditSummaryService(db).GetAsync(member.Id))!;
        Assert.Equal(8, summary.PersonalTrainingTotal);
        Assert.Equal(4, summary.GroupTotal);
        Assert.Equal(12, summary.PackageTotal);
        Assert.True(MemberPackageEntitlement.IncludesPersonalTraining(member));
    }

    private static ApplicationDbContext Db() => new(new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
    private static MemberProfile Seed(ApplicationDbContext db, int pt, int group)
    {
        var member = new MemberProfile {
            ApplicationUser = new ApplicationUser { UserName = Guid.NewGuid().ToString() },
            MembershipPackage = new MembershipPackage { Code = MembershipPackageCode.Pro, WeeklyClassLimit = 4 },
            ServicePackageVariant = new ServicePackageVariant { ServicePackage = new ServicePackage { Name = "QA" }, PersonalTrainingSessionCount = pt, GroupClassCreditCount = group },
            RemainingClassCredits = pt + group, MembershipStartsAtUtc = DateTime.UtcNow.AddDays(-10), MembershipEndsAtUtc = DateTime.UtcNow.AddDays(20)
        };
        db.Add(member); return member;
    }
}
