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
        Assert.Equal(2, await db.PersonalTrainingSessions.CountAsync());
    }
}
