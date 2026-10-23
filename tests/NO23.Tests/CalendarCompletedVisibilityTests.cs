using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NO23.Web.Data;
using NO23.Web.Domain.Entities;
using NO23.Web.Domain.Enums;
using NO23.Web.Services;
using NO23.Web.ViewModels.Admin;
using NO23.Web.ViewModels.TrainerPanel;

namespace NO23.Tests;

public class CalendarCompletedVisibilityTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task AdminAndTrainer_HideCompletedByDefaultAndKeepHistoryOnRequest(bool includeCompleted)
    {
        await using var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var week = ClubTime.Monday(ClubTime.Now);
        var trainer = new Trainer { FirstName = "QA", ApplicationUserId = "trainer-user" };
        var member = new MemberProfile { ApplicationUser = new ApplicationUser { FirstName = "QA" },
            MembershipPackage = new MembershipPackage { WeeklyClassLimit = 4 }, AssignedTrainer = trainer, RemainingClassCredits = 10 };
        foreach (var completed in new[] { false, true })
        {
            db.PersonalTrainingSessions.Add(new() { MemberProfile = member, Trainer = trainer,
                StartsAtUtc = ClubTime.ToUtc(week.AddDays(1).AddHours(completed ? 10 : 11)),
                Status = completed ? PersonalTrainingSessionStatus.Completed : PersonalTrainingSessionStatus.Scheduled,
                CreditConsumed = completed });
            db.ClassSessions.Add(new() { GroupClass = new GroupClass { Name = "QA", Trainer = trainer },
                StartsAtUtc = ClubTime.ToUtc(week.AddHours(completed ? 9 : 10)),
                Status = completed ? ClassSessionStatus.Completed : ClassSessionStatus.Scheduled });
        }
        db.PersonalTrainingSessions.Add(new() { MemberProfile = member, Trainer = trainer,
            StartsAtUtc = ClubTime.ToUtc(week.AddDays(2).AddHours(10)), Status = PersonalTrainingSessionStatus.Cancelled });
        db.ClassSessions.Add(new() { GroupClass = new GroupClass { Name = "Cancelled", Trainer = trainer },
            StartsAtUtc = ClubTime.ToUtc(week.AddDays(2).AddHours(11)), Status = ClassSessionStatus.Cancelled });
        await db.SaveChangesAsync();
        var service = new PersonalTrainingCalendarService(db);
        var admin = new NO23.Web.Areas.Admin.Controllers.CalendarController(db, service, new ClassReservationService(db));
        var adminResult = Assert.IsType<ViewResult>(await admin.Index(week, trainer.Id, includeCompleted: includeCompleted));
        var adminModel = Assert.IsType<WeeklyCalendarViewModel>(adminResult.Model);
        Assert.Equal(includeCompleted ? 2 : 1, adminModel.Personal.Count);
        Assert.Equal(includeCompleted ? 2 : 1, adminModel.Groups.Count);
        var coach = new NO23.Web.Areas.Trainer.Controllers.CalendarController(db, service) {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(
                new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, "trainer-user")], "qa")) } } };
        var trainerResult = Assert.IsType<ViewResult>(await coach.Index(week, includeCompleted: includeCompleted));
        var trainerModel = Assert.IsType<TrainerCalendarViewModel>(trainerResult.Model);
        Assert.Equal(includeCompleted ? 2 : 1, trainerModel.Sessions.Count);
        Assert.Equal(includeCompleted ? 2 : 1, trainerModel.Groups.Count);
        Assert.Equal(10, member.RemainingClassCredits); // Changing the display never changes the balance.
        Assert.Equal(3, await db.PersonalTrainingSessions.CountAsync()); // Cancelled records remain in history.
    }

    [Fact]
    public async Task ReactivatingAGroup_DoesNotBringItsCancelledSessionsBackToCalendar()
    {
        await using var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var week = ClubTime.Monday(ClubTime.Now.AddDays(14));
        var trainer = new Trainer { FirstName = "QA" };
        var member = new MemberProfile { ApplicationUser = new ApplicationUser { UserName = "qa-member" },
            MembershipPackage = new MembershipPackage { WeeklyClassLimit = 4 }, RemainingClassCredits = 3 };
        var group = new GroupClass { Name = "QA", Trainer = trainer, IsActive = true };
        var lesson = new ClassSession { GroupClass = group, StartsAtUtc = ClubTime.ToUtc(week.AddHours(11)) };
        lesson.Reservations.Add(new ClassReservation { MemberProfile = member });
        db.Add(lesson); await db.SaveChangesAsync();
        Assert.True((await new GroupClassRemovalService(db).DeactivateAsync(group.Id)).Succeeded);
        Assert.Equal(ClassSessionStatus.Cancelled, lesson.Status);
        Assert.Equal(4, member.RemainingClassCredits);
        group.IsActive = true; await db.SaveChangesAsync();
        var controller = new NO23.Web.Areas.Admin.Controllers.CalendarController(db,
            new PersonalTrainingCalendarService(db), new ClassReservationService(db));
        foreach (var showCompleted in new[] { false, true })
        {
            var result = Assert.IsType<ViewResult>(await controller.Index(week, trainer.Id, includeCompleted: showCompleted));
            Assert.Empty(Assert.IsType<WeeklyCalendarViewModel>(result.Model).Groups);
        }
        Assert.Single(await db.ClassSessions.ToListAsync());
        Assert.Equal(ClassReservationStatus.Cancelled, Assert.Single(lesson.Reservations).Status);
        Assert.Equal(4, member.RemainingClassCredits); // Re-activation must not reserve the refunded right again.
    }
}
