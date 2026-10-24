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

public class AppointmentSchedulingTests
{
    [Fact]
    public async Task GuestPreferenceIsNotABooking_ConfirmedAppointmentNeedsNoMemberOrCredits()
    {
        await using var db = Db();
        var (request, trainer, start) = await Seed(db);
        var before = await AdminCalendar(db, ClubTime.ToLocal(start));
        Assert.Empty(before.Appointments);
        var preferredDate = request.PreferredDate;
        var result = await new AppointmentSchedulingService(db).ScheduleAsync(Input(request, trainer, start), "admin");
        Assert.True(result.Succeeded, result.Message);
        Assert.Equal(start, request.ScheduledAtUtc);
        Assert.Equal(trainer.Id, request.ScheduledTrainerId);
        Assert.Equal(AppointmentScheduleStatus.Scheduled, request.ScheduleStatus);
        Assert.Equal(ServicePackageApplicationStatus.Approved, request.Status);
        Assert.Equal(preferredDate, request.PreferredDate);
        Assert.Single(await db.AppointmentScheduleHistories.ToListAsync());
        Assert.Empty(db.MemberProfiles);
        Assert.Empty(db.PersonalTrainingSessions);
        Assert.Empty(db.ClassReservations);
        Assert.Single((await AdminCalendar(db, ClubTime.ToLocal(start))).Appointments);
    }

    [Theory]
    [InlineData("past")]
    [InlineData("missing-date")]
    [InlineData("duration-low")]
    [InlineData("duration-high")]
    [InlineData("inactive-trainer")]
    [InlineData("unknown-trainer")]
    [InlineData("stale")]
    [InlineData("missing-version")]
    [InlineData("long-note")]
    [InlineData("no-actor")]
    public async Task InvalidSchedule_DoesNotChangeRequestOrCreateHistory(string problem)
    {
        await using var db = Db();
        var (request, trainer, start) = await Seed(db);
        var input = Input(request, trainer, start);
        switch (problem)
        {
            case "past": input.StartsAt = ClubTime.Now.AddDays(-1); break;
            case "missing-date": input.StartsAt = default; break;
            case "duration-low": input.DurationMinutes = 14; break;
            case "duration-high": input.DurationMinutes = 241; break;
            case "inactive-trainer": trainer.IsActive = false; await db.SaveChangesAsync(); break;
            case "unknown-trainer": input.TrainerId = int.MaxValue; break;
            case "stale": input.ExpectedVersion = Guid.NewGuid(); break;
            case "missing-version": input.ExpectedVersion = null; break;
            case "long-note": input.Note = new string('x', 1001); break;
        }
        var result = await new AppointmentSchedulingService(db).ScheduleAsync(input, problem == "no-actor" ? "" : "admin");
        Assert.False(result.Succeeded);
        Assert.Null(request.ScheduledAtUtc);
        Assert.Equal(ServicePackageApplicationStatus.Pending, request.Status);
        Assert.Empty(db.AppointmentScheduleHistories);
    }

    [Theory]
    [InlineData("group", true, false)]
    [InlineData("personal", true, false)]
    [InlineData("appointment", true, false)]
    [InlineData("group", false, true)]
    [InlineData("personal", false, true)]
    [InlineData("appointment", false, true)]
    public async Task Schedule_ChecksOnlyActiveBookingsForSameTrainer(string kind, bool scheduled, bool allowed)
    {
        await using var db = Db();
        var (request, trainer, start) = await Seed(db);
        if (kind == "group") db.ClassSessions.Add(new() { GroupClass = new GroupClass { Name = "Reformer", Trainer = trainer, DurationMinutes = 50 },
            StartsAtUtc = start.AddMinutes(-10), Status = scheduled ? ClassSessionStatus.Scheduled : ClassSessionStatus.Cancelled });
        if (kind == "personal") db.PersonalTrainingSessions.Add(new() { Trainer = trainer, MemberProfile = Member(),
            StartsAtUtc = start.AddMinutes(-10), DurationMinutes = 50, Status = scheduled ? PersonalTrainingSessionStatus.Scheduled : PersonalTrainingSessionStatus.Cancelled });
        if (kind == "appointment") db.AppointmentRequests.Add(new() { FullName = "Diğer", ScheduledTrainer = trainer,
            ScheduledAtUtc = start.AddMinutes(-10), DurationMinutes = 50,
            ScheduleStatus = scheduled ? AppointmentScheduleStatus.Scheduled : AppointmentScheduleStatus.Cancelled });
        await db.SaveChangesAsync();
        var result = await new AppointmentSchedulingService(db).ScheduleAsync(Input(request, trainer, start), "admin");
        Assert.Equal(allowed, result.Succeeded);
    }

    [Theory]
    [InlineData(-30, true)]
    [InlineData(-29, false)]
    [InlineData(0, false)]
    [InlineData(29, false)]
    [InlineData(30, true)]
    public async Task AdjacentMeetingsAreAllowed_ButAnyOverlapIsRejected(int offset, bool allowed)
    {
        await using var db = Db();
        var (request, trainer, start) = await Seed(db);
        db.AppointmentRequests.Add(new() { ScheduledTrainer = trainer, ScheduledAtUtc = start.AddMinutes(offset),
            DurationMinutes = 30, ScheduleStatus = AppointmentScheduleStatus.Scheduled });
        await db.SaveChangesAsync();
        Assert.Equal(allowed, (await new AppointmentSchedulingService(db).ScheduleAsync(Input(request, trainer, start), "admin")).Succeeded);
    }

    [Fact]
    public async Task RescheduleAndCancel_PreserveHistoryAndBalance_StalePostDoesNotOverwrite()
    {
        await using var db = Db();
        var (request, trainer, start) = await Seed(db);
        var member = Member(); db.MemberProfiles.Add(member); await db.SaveChangesAsync();
        var service = new AppointmentSchedulingService(db);
        var firstInput = Input(request, trainer, start);
        Assert.True((await service.ScheduleAsync(firstInput, "admin")).Succeeded);
        Assert.False((await service.ScheduleAsync(firstInput, "admin")).Succeeded);
        var move = Input(request, trainer, start.AddHours(2));
        Assert.True((await service.ScheduleAsync(move, "admin")).Succeeded);
        Assert.Equal(2, request.ScheduleHistory.Count);
        var change = request.ScheduleHistory.Last();
        Assert.Equal(start, change.PreviousStartsAtUtc);
        Assert.Equal(start.AddHours(2), change.StartsAtUtc);
        Assert.True((await service.FinishAsync(request.Id, request.Version, AppointmentScheduleStatus.Cancelled, "admin")).Succeeded);
        Assert.Equal(3, request.ScheduleHistory.Count);
        Assert.Empty((await AdminCalendar(db, ClubTime.ToLocal(start), includeCompleted: true)).Appointments);
        Assert.Equal(17, member.RemainingClassCredits);
        Assert.Empty(db.PersonalTrainingSessions);
        Assert.Empty(db.ClassReservations);
    }

    [Fact]
    public async Task ClosingRequest_CancelsBookingAndFreesTime_ReopeningDoesNotRestoreBooking()
    {
        await using var db = Db();
        var (request, trainer, start) = await Seed(db);
        var service = new AppointmentSchedulingService(db);
        Assert.True((await service.ScheduleAsync(Input(request, trainer, start), "admin")).Succeeded);
        Assert.True((await service.UpdateRequestStatusAsync(request.Id, ServicePackageApplicationStatus.Closed, "admin")).Succeeded);
        Assert.Equal(AppointmentScheduleStatus.Cancelled, request.ScheduleStatus);
        Assert.True((await service.UpdateRequestStatusAsync(request.Id, ServicePackageApplicationStatus.Pending, "admin")).Succeeded);
        Assert.Equal(AppointmentScheduleStatus.Cancelled, request.ScheduleStatus);
        Assert.Empty((await AdminCalendar(db, ClubTime.ToLocal(start))).Appointments);
        Assert.True((await service.ScheduleAsync(Input(request, trainer, start), "admin")).Succeeded);
    }

    [Fact]
    public async Task FutureMeetingsCannotComplete_PastCompletionsRemainAvailableInHistory()
    {
        await using var db = Db();
        var (request, trainer, start) = await Seed(db);
        var service = new AppointmentSchedulingService(db);
        await service.ScheduleAsync(Input(request, trainer, start), "admin");
        Assert.False((await service.FinishAsync(request.Id, request.Version, AppointmentScheduleStatus.Completed, "admin")).Succeeded);
        request.ScheduledAtUtc = DateTime.UtcNow.AddHours(-2); await db.SaveChangesAsync();
        Assert.True((await service.FinishAsync(request.Id, request.Version, AppointmentScheduleStatus.Completed, "admin")).Succeeded);
        var local = ClubTime.ToLocal(request.ScheduledAtUtc.Value);
        Assert.Empty((await AdminCalendar(db, local)).Appointments);
        Assert.Single((await AdminCalendar(db, local, includeCompleted: true)).Appointments);
        Assert.False((await service.ScheduleAsync(Input(request, trainer, start), "admin")).Succeeded);
    }

    [Fact]
    public async Task TrainerAndMemberFilters_AndDetailAuthorization_DoNotLeakGuestDetails()
    {
        await using var db = Db();
        var (request, trainer, start) = await Seed(db);
        await new AppointmentSchedulingService(db).ScheduleAsync(Input(request, trainer, start), "admin");
        var controller = new NO23.Web.Areas.Trainer.Controllers.CalendarController(db, new(db)) { ControllerContext = Context("trainer") };
        var model = Assert.IsType<TrainerCalendarViewModel>(Assert.IsType<ViewResult>(await controller.Index(ClubTime.ToLocal(start))).Model);
        Assert.Single(model.Appointments);
        var filtered = Assert.IsType<TrainerCalendarViewModel>(Assert.IsType<ViewResult>(await controller.Index(ClubTime.ToLocal(start), memberId: 50)).Model);
        Assert.Empty(filtered.Appointments);
        Assert.Empty((await AdminCalendar(db, ClubTime.ToLocal(start), memberId: 50)).Appointments);
        Assert.Empty((await AdminCalendar(db, ClubTime.ToLocal(start), trainerId: 500)).Appointments);
        var details = new NO23.Web.Areas.Trainer.Controllers.AppointmentRequestsController(db) { ControllerContext = Context("trainer") };
        Assert.IsType<ViewResult>(await details.Details(request.Id));
        var other = new Trainer { ApplicationUserId = "other", FirstName = "Diğer" }; db.Trainers.Add(other); await db.SaveChangesAsync();
        details.ControllerContext = Context("other");
        Assert.IsType<NotFoundResult>(await details.Details(request.Id));
        other.IsActive = false; await db.SaveChangesAsync();
        Assert.IsType<ForbidResult>(await details.Details(request.Id));
    }

    [Fact]
    public async Task ScheduledMeetingBlocksSingleWeeklyAndBulkPersonalLessons_AndWeeklyGroups()
    {
        await using var db = Db();
        var (request, trainer, start) = await Seed(db);
        await new AppointmentSchedulingService(db).ScheduleAsync(Input(request, trainer, start), "admin");
        var member = Member(); member.AssignedTrainer = trainer; db.MemberProfiles.Add(member); await db.SaveChangesAsync();
        var personal = new PersonalTrainingCalendarService(db);
        Assert.False((await personal.CreateByAdminAsync(trainer.Id, member.Id, start, 50, null)).Succeeded);
        var weekly = new WeeklyPersonalSessionInput { MemberProfileId = member.Id, Week = ClubTime.ToLocal(start),
            Days = [ClubTime.ToLocal(start).DayOfWeek], Time = TimeOnly.FromDateTime(ClubTime.ToLocal(start)), Weeks = 1 };
        Assert.False((await personal.CreateWeeklyByAdminAsync(trainer.Id, weekly)).Succeeded);
        var group = new GroupClass { Name = "Grup", Trainer = trainer, DurationMinutes = 50 }; db.GroupClasses.Add(group);
        var session = new PersonalTrainingSession { Trainer = trainer, MemberProfile = member, StartsAtUtc = start.AddHours(1), DurationMinutes = 50 };
        db.PersonalTrainingSessions.Add(session); await db.SaveChangesAsync();
        Assert.False((await new WeeklyGroupSchedulingService(db).CreateAsync(new() { GroupClassId = group.Id, Week = ClubTime.ToLocal(start),
            Days = [ClubTime.ToLocal(start).DayOfWeek], Time = TimeOnly.FromDateTime(ClubTime.ToLocal(start)) })).Succeeded);
        Assert.False((await new CalendarBulkOperationService(db).ApplyAsync(new() { PersonalIds = [session.Id],
            Operation = CalendarBulkOperation.UpdateTime, Time = TimeOnly.FromDateTime(ClubTime.ToLocal(start)) }, "admin")).Succeeded);
        Assert.Equal(start.AddHours(1), session.StartsAtUtc);
        Assert.Equal(17, member.RemainingClassCredits);
        Assert.Empty(db.ClassSessions);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SingleGroupCreateAndEdit_CannotOverlapAnAppointment(bool edit)
    {
        await using var db = Db();
        var (request, trainer, start) = await Seed(db);
        await new AppointmentSchedulingService(db).ScheduleAsync(Input(request, trainer, start), "admin");
        var group = new GroupClass { Name = "Grup", Trainer = trainer, DurationMinutes = 50, Capacity = 8 };
        db.GroupClasses.Add(group);
        var existing = new ClassSession { GroupClass = group, StartsAtUtc = start.AddHours(1) };
        if (edit) db.ClassSessions.Add(existing);
        await db.SaveChangesAsync();
        var controller = new NO23.Web.Areas.Admin.Controllers.ClassSessionsController(db, new(db), null!);
        var input = new ClassSessionFormViewModel { Id = edit ? existing.Id : 0, GroupClassId = group.Id,
            StartsAtLocal = ClubTime.ToLocal(start), Status = ClassSessionStatus.Scheduled };
        var result = edit ? await controller.Edit(existing.Id, input) : await controller.Create(input);
        Assert.IsType<ViewResult>(result);
        Assert.NotEmpty(controller.ModelState[nameof(input.StartsAtLocal)]!.Errors);
        Assert.Equal(edit ? 1 : 0, await db.ClassSessions.CountAsync());
        if (edit) Assert.Equal(start.AddHours(1), existing.StartsAtUtc);
    }

    private static ControllerContext Context(string userId) => new() { HttpContext = new DefaultHttpContext { User =
        new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, userId)], "test")) } };
    private static MemberProfile Member() => new() { ApplicationUser = new ApplicationUser { FirstName = "Test", LastName = "Üye" },
        MembershipPackage = new MembershipPackage { Code = MembershipPackageCode.Start, WeeklyClassLimit = 4 }, RemainingClassCredits = 17 };
    private static ApplicationDbContext Db() => new(new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
    private static async Task<(AppointmentRequest Request, Trainer Trainer, DateTime Start)> Seed(ApplicationDbContext db)
    {
        var trainer = new Trainer { FirstName = "Deneme", LastName = "Eğitmen", ApplicationUserId = "trainer" };
        var local = ClubTime.Now.Date.AddDays(14).AddHours(10);
        var request = new AppointmentRequest { FullName = "Deneme Ziyaretçi", PhoneNumber = "05300000000", Service = "Ön Görüşme",
            PreferredDate = DateOnly.FromDateTime(local), PreferredTime = TimeOnly.FromDateTime(local) };
        db.Trainers.Add(trainer); db.AppointmentRequests.Add(request); await db.SaveChangesAsync();
        return (request, trainer, ClubTime.ToUtc(local));
    }
    private static AppointmentScheduleViewModel Input(AppointmentRequest request, Trainer trainer, DateTime start) => new()
    { Id = request.Id, TrainerId = trainer.Id, StartsAt = ClubTime.ToLocal(start), DurationMinutes = 30, ExpectedVersion = request.Version };
    private static async Task<WeeklyCalendarViewModel> AdminCalendar(ApplicationDbContext db, DateTime date,
        int? trainerId = null, int? memberId = null, bool includeCompleted = false) => Assert.IsType<WeeklyCalendarViewModel>(
            Assert.IsType<ViewResult>(await new NO23.Web.Areas.Admin.Controllers.CalendarController(db, new(db), new(db))
                .Index(date, trainerId, memberId, includeCompleted)).Model);
}
