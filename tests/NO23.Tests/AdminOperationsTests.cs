using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NO23.Web.Data;
using NO23.Web.Data.Seed;
using NO23.Web.Domain.Entities;
using NO23.Web.Domain.Enums;
using NO23.Web.Services;
using NO23.Web.Services.Email;
using NO23.Web.ViewModels.Admin;
using NO23.Web.ViewModels.Member;
using NO23.Web.Areas.Admin.Controllers;
using NO23.Web.Controllers;

namespace NO23.Tests;

public class AdminOperationsTests
{
    [Fact]
    public async Task WeeklyProgram_CreatesSelectedDaysAcrossWeeks_AndRejectsDuplicateBatch()
    {
        await using var db = Db();
        var (group, _) = await Seed(db);
        var service = new WeeklyGroupSchedulingService(db);
        var input = new WeeklyGroupInput { GroupClassId = group.Id, Week = ClubTime.Now.AddDays(14), Days = [DayOfWeek.Monday, DayOfWeek.Thursday], Weeks = 3, Time = new(18, 30) };
        Assert.True((await service.CreateAsync(input)).Succeeded);
        var sessions = await db.ClassSessions.OrderBy(x => x.StartsAtUtc).ToListAsync();
        Assert.Equal(6, sessions.Count);
        Assert.All(sessions, x => Assert.Equal(new TimeSpan(15, 30, 0), x.StartsAtUtc.TimeOfDay));
        Assert.Equal(7, (sessions[2].StartsAtUtc - sessions[0].StartsAtUtc).TotalDays);
        Assert.False((await service.CreateAsync(input)).Succeeded);
        Assert.Equal(6, await db.ClassSessions.CountAsync());
    }

    [Fact]
    public async Task GroupAndPersonalLessons_RejectTrainerOverlapInBothDirections()
    {
        await using var db = Db();
        var (group, member) = await Seed(db);
        var week = ClubTime.Monday(ClubTime.Now.AddDays(14));
        var start = ClubTime.ToUtc(week.AddHours(18));
        var personal = new PersonalTrainingCalendarService(db);
        Assert.True((await personal.CreateAsync(group.TrainerId, member.Id, start, 60, null)).Succeeded);
        var input = new WeeklyGroupInput { GroupClassId = group.Id, Week = week, Days = [DayOfWeek.Monday, DayOfWeek.Friday], Time = new(18, 30) };
        Assert.False((await new WeeklyGroupSchedulingService(db).CreateAsync(input)).Succeeded);
        Assert.Empty(db.ClassSessions);
        db.ClassSessions.Add(new ClassSession { GroupClassId = group.Id, StartsAtUtc = start.AddDays(1), Status = ClassSessionStatus.Scheduled });
        await db.SaveChangesAsync();
        Assert.False((await personal.CreateAsync(group.TrainerId, member.Id, start.AddDays(1).AddMinutes(30), 60, null)).Succeeded);
    }

    [Fact]
    public async Task AdminParticipant_AddChecksCapacityAndDuplicate_RefundsWhenRemoved()
    {
        await using var db = Db();
        var (group, member) = await Seed(db);
        var session = new ClassSession { GroupClassId = group.Id, StartsAtUtc = DateTime.UtcNow.AddDays(1), CapacityOverride = 1, Status = ClassSessionStatus.Scheduled };
        db.ClassSessions.Add(session);
        await db.SaveChangesAsync();
        var service = new ClassReservationService(db);
        Assert.True((await service.ReserveAsync(member.ApplicationUserId, session.Id)).Succeeded);
        Assert.False((await service.ReserveAsync(member.ApplicationUserId, session.Id)).Succeeded);
        Assert.Equal(4, member.RemainingClassCredits);
        var second = new MemberProfile { ApplicationUser = new ApplicationUser { UserName = "second" }, MembershipPackage = member.MembershipPackage, RemainingClassCredits = 5 };
        db.MemberProfiles.Add(second);
        await db.SaveChangesAsync();
        Assert.False((await service.ReserveAsync(second.ApplicationUserId, session.Id)).Succeeded);
        Assert.Equal(5, second.RemainingClassCredits);
        Assert.True((await service.CancelByAdminAsync(session.Id, (await db.ClassReservations.SingleAsync()).Id)).Succeeded);
        Assert.Equal(5, member.RemainingClassCredits);
    }

    [Fact]
    public async Task MonthlyMeasurements_PreserveHistoryAndMemberCalories()
    {
        await using var db = Db();
        var (_, member) = await Seed(db);
        var today = DateOnly.FromDateTime(ClubTime.Now);
        db.MemberProgressEntries.Add(new MemberProgressEntry { MemberProfileId = member.Id, EntryDate = today.AddMonths(-1), BodyWeightKg = 85 });
        db.MemberProgressEntries.Add(new MemberProgressEntry { MemberProfileId = member.Id, EntryDate = today, CaloriesConsumed = 2100 });
        await db.SaveChangesAsync();
        var controller = Setup(new MemberProgressController(db, new MemberProgressTrackingService(db)));
        Assert.IsType<RedirectToActionResult>(await controller.Save(member.Id, new MemberProgressEntryInputViewModel { EntryDate = today, BodyWeightKg = 82, BodyFatPercent = 20 }));
        Assert.Equal(2100, (await db.MemberProgressEntries.SingleAsync(x => x.EntryDate == today)).CaloriesConsumed);
        var model = Assert.IsType<MemberProgressPage>(Assert.IsType<ViewResult>(await controller.Index(member.Id, null, null, null)).Model);
        Assert.Equal(2, model.Entries.Count);
        Assert.Equal(-3m, model.Entries[0].BodyWeightKg - model.Entries[1].BodyWeightKg);
        await controller.Save(member.Id, new MemberProgressEntryInputViewModel { EntryDate = today, BodyWeightKg = 81 });
        Assert.Equal(2, await db.MemberProgressEntries.CountAsync());
    }

    [Theory]
    [InlineData("unrelated", "Member", false)]
    [InlineData("member", "Member", true)]
    [InlineData("coach", "Trainer", true)]
    [InlineData("unrelated", "Admin", true)]
    [InlineData("unrelated", "Trainer", false)]
    public async Task ProgressPhotos_AreVisibleOnlyToAuthorizedPeople(string user, string role, bool allowed)
    {
        await using var db = Db();
        var (group, member) = await Seed(db);
        group.Trainer.ApplicationUser = new ApplicationUser { Id = "coach", UserName = "coach" };
        var photo = new MemberProgressPhoto { MemberProfileId = member.Id, TakenOn = DateOnly.FromDateTime(ClubTime.Now), Data = [1, 2, 3], ContentType = "image/png" };
        db.MemberProgressPhotos.Add(photo);
        await db.SaveChangesAsync();
        var controller = Setup(new ProgressPhotosController(db));
        controller.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, user), new Claim(ClaimTypes.Role, role)], "test"));
        var result = await controller.Image(photo.Id);
        if (allowed) Assert.IsType<FileContentResult>(result);
        else Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public void PhotoValidation_RejectsActiveContentAndOversizeFiles()
    {
        Assert.Null(ProgressPhotoValidation.ContentType(System.Text.Encoding.UTF8.GetBytes("<svg onload='alert(1)'></svg>")));
        Assert.Null(ProgressPhotoValidation.ContentType(new byte[ProgressPhotoValidation.MaxBytes + 1]));
        var png = Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+aN1sAAAAASUVORK5CYII=");
        Assert.Equal("image/png", ProgressPhotoValidation.ContentType(png));
    }

    [Fact]
    public async Task DisabledEmail_DoesNotClaimInvitationWasSent()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContext<ApplicationDbContext>(o => o.UseInMemoryDatabase(Guid.NewGuid().ToString()));
        services.AddIdentityCore<ApplicationUser>().AddRoles<IdentityRole>().AddEntityFrameworkStores<ApplicationDbContext>();
        await using var provider = services.BuildServiceProvider();
        var db = provider.GetRequiredService<ApplicationDbContext>();
        var manager = provider.GetRequiredService<UserManager<ApplicationUser>>();
        var trainer = new Trainer { FirstName = "Test", LastName = "Coach", Specialty = "PT", ApplicationUser = new ApplicationUser { UserName = "coach", Email = "coach@test.local" } };
        db.Trainers.Add(trainer);
        await db.SaveChangesAsync();
        var controller = Setup(new NO23.Web.Areas.Admin.Controllers.TrainersController(db, manager, new DisabledEmailSender(NullLogger<DisabledEmailSender>.Instance)));
        Assert.IsType<RedirectToActionResult>(await controller.ResendInvitation(trainer.Id));
        Assert.Contains("gönderilemedi", controller.TempData["StatusMessage"]?.ToString());
        Assert.Contains("yapılandırılmamış", controller.TempData["InvitationWarning"]?.ToString());
    }

    private static T Setup<T>(T controller) where T : Controller
    {
        controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
        controller.TempData = new TempDataDictionary(controller.HttpContext, new MemoryTempData());
        return controller;
    }

    private sealed class MemoryTempData : ITempDataProvider
    {
        public IDictionary<string, object> LoadTempData(HttpContext context) => new Dictionary<string, object>();
        public void SaveTempData(HttpContext context, IDictionary<string, object> values) { }
    }

    private static ApplicationDbContext Db() => new(new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
    private static async Task<(GroupClass Group, MemberProfile Member)> Seed(ApplicationDbContext db)
    {
        var trainer = new Trainer { FirstName = "Test", LastName = "Coach", Specialty = "PT" };
        var group = new GroupClass { Name = "Reformer", Trainer = trainer, DurationMinutes = 60, Capacity = 8 };
        var member = new MemberProfile { ApplicationUser = new ApplicationUser { Id = "member", UserName = "member", FirstName = "Test", LastName = "Member" },
            MembershipPackage = new MembershipPackage { Name = "PRO", Audience = "Test", Description = "Test", WeeklyClassLimit = 4 }, RemainingClassCredits = 5, AssignedTrainer = trainer };
        db.GroupClasses.Add(group);
        db.MemberProfiles.Add(member);
        await db.SaveChangesAsync();
        return (group, member);
    }
}
