using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NO23.Web.Controllers;
using NO23.Web.Data;
using NO23.Web.Domain.Entities;
using NO23.Web.Domain.Enums;
using NO23.Web.Services;
using NO23.Web.ViewModels.Classes;

namespace NO23.Tests;

public class PublicClassScheduleTests
{
    [Fact]
    public async Task Index_GroupsRequestedWeekByTurkishDayNames()
    {
        await using var db = CreateDbContext();
        var currentWeekStart = ClubTime.Monday(ClubTime.Today);
        var requestedWeekStart = currentWeekStart.AddDays(14);
        await SeedSessionAsync(db, requestedWeekStart.AddHours(9));
        await SeedSessionAsync(db, requestedWeekStart.AddDays(2).AddHours(18));
        await SeedSessionAsync(db, requestedWeekStart.AddDays(7).AddHours(9));

        var model = await IndexAsync(db, DateOnly.FromDateTime(requestedWeekStart.AddDays(3)));

        Assert.Equal(requestedWeekStart, model.WeekStart);
        Assert.Equal(7, model.WeekDays.Count);
        Assert.Equal("Pazartesi", model.WeekDays[0].DayName);
        Assert.Equal("Çarşamba", model.WeekDays[2].DayName);
        Assert.Equal("Pazar", model.WeekDays[6].DayName);
        Assert.Single(model.WeekDays[0].Sessions);
        Assert.Equal("Çarşamba", Assert.Single(model.WeekDays[2].Sessions).DayName);
        Assert.Equal(2, model.WeekDays.Sum(day => day.Sessions.Count));
        Assert.Equal(DateOnly.FromDateTime(requestedWeekStart.AddDays(-7)), model.PreviousWeek);
        Assert.Equal(DateOnly.FromDateTime(requestedWeekStart.AddDays(7)), model.NextWeek);
        Assert.False(model.IsCurrentWeek);
    }

    [Fact]
    public async Task Index_ClampsPastWeeksToCurrentWeek()
    {
        await using var db = CreateDbContext();

        var model = await IndexAsync(db, DateOnly.FromDateTime(ClubTime.Today.AddDays(-30)));

        Assert.Equal(ClubTime.Monday(ClubTime.Today), model.WeekStart);
        Assert.True(model.IsCurrentWeek);
        Assert.Null(model.PreviousWeek);
    }

    private static async Task<ClassesIndexViewModel> IndexAsync(ApplicationDbContext db, DateOnly week)
    {
        var result = await new ClassesController(db).Index(week);
        return Assert.IsType<ClassesIndexViewModel>(Assert.IsType<ViewResult>(result).Model);
    }

    private static async Task SeedSessionAsync(ApplicationDbContext db, DateTime startsAtLocal)
    {
        var groupClass = await db.GroupClasses.FirstOrDefaultAsync() ?? new GroupClass
        {
            Name = "Bootcamp",
            Trainer = new Trainer { FirstName = "Test", LastName = "Coach", Specialty = "Group" },
            DurationMinutes = 50,
            Capacity = 10,
            IsActive = true
        };
        db.ClassSessions.Add(new ClassSession
        {
            GroupClass = groupClass,
            StartsAtUtc = ClubTime.ToUtc(startsAtLocal),
            Status = ClassSessionStatus.Scheduled
        });
        await db.SaveChangesAsync();
    }

    private static ApplicationDbContext CreateDbContext() => new(
        new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);
}
