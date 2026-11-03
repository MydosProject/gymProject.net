using Microsoft.EntityFrameworkCore;
using NO23.Web.Data;
using NO23.Web.Domain.Entities;
using NO23.Web.Domain.Enums;
using NO23.Web.Services;
using NO23.Web.ViewModels.Member;

namespace NO23.Tests;

public class MemberClassScheduleTests
{
    private static readonly DateTime Today = ClubTime.Today;
    private static readonly DateTime NowUtc = ClubTime.ToUtc(Today.AddHours(12));

    [Fact]
    public async Task WeekView_SplitsTheFourteenDayWindowIntoTwoWeeks()
    {
        await using var db = Db();
        var bootcamp = Class(db, "Bootcamp", LessonCategory.Performance);
        Session(db, bootcamp, Today.AddHours(9));
        Session(db, bootcamp, Today.AddHours(18));
        Session(db, bootcamp, Today.AddDays(6).AddHours(18));
        Session(db, bootcamp, Today.AddDays(7).AddHours(9));
        Session(db, bootcamp, Today.AddDays(13).AddHours(20));
        Session(db, bootcamp, Today.AddDays(14).AddHours(9));
        await db.SaveChangesAsync();
        var service = new MemberClassScheduleService(db);

        var first = await service.GetAsync(null, new(), NowUtc);
        Assert.Equal(MemberClassScheduleViewModel.WeekView, first.View);
        Assert.Equal(Today, first.RangeStart);
        Assert.Equal(Today.AddDays(6), first.RangeEnd);
        Assert.Equal(7, first.Days.Count);
        Assert.Equal(2, first.Sessions.Count);
        Assert.Null(first.PreviousDate);
        Assert.Equal(DateOnly.FromDateTime(Today.AddDays(7)), first.NextDate);

        var second = await service.GetAsync(null, new(DateOnly.FromDateTime(Today.AddDays(9))), NowUtc);
        Assert.Equal(Today.AddDays(7), second.RangeStart);
        Assert.Equal(Today.AddDays(13), second.RangeEnd);
        Assert.Equal(2, second.Sessions.Count);
        Assert.Null(second.NextDate);
        Assert.Equal(DateOnly.FromDateTime(Today), second.PreviousDate);

        var clamped = await service.GetAsync(null, new(DateOnly.FromDateTime(Today.AddDays(40))), NowUtc);
        Assert.Equal(Today.AddDays(13), clamped.SelectedDate);
        Assert.Equal(Today.AddDays(7), clamped.RangeStart);
    }

    [Fact]
    public async Task DayView_ShowsOnlySelectedDay_WithTurkishDayNameAndNeighbourDays()
    {
        await using var db = Db();
        var bootcamp = Class(db, "Bootcamp", LessonCategory.Performance);
        Session(db, bootcamp, Today.AddDays(7).AddHours(9));
        Session(db, bootcamp, Today.AddDays(7).AddHours(19));
        Session(db, bootcamp, Today.AddDays(8).AddHours(9));
        await db.SaveChangesAsync();

        var model = await new MemberClassScheduleService(db).GetAsync(null,
            new(DateOnly.FromDateTime(Today.AddDays(7)), MemberClassScheduleViewModel.DayView), NowUtc);

        Assert.True(model.IsDaily);
        var day = Assert.Single(model.Days);
        Assert.Equal(Today.AddDays(7), day.Date);
        Assert.Equal(Today.AddDays(7).ToString("dddd", System.Globalization.CultureInfo.GetCultureInfo("tr-TR")), day.DayName);
        Assert.Equal(2, day.Sessions.Count);
        Assert.Equal(DateOnly.FromDateTime(Today.AddDays(6)), model.PreviousDate);
        Assert.Equal(DateOnly.FromDateTime(Today.AddDays(8)), model.NextDate);
    }

    [Fact]
    public async Task Filters_NarrowByLessonTypeAndClass_AndShowCapacityAndOwnReservation()
    {
        await using var db = Db();
        var reformer = Class(db, "Reformer Flow", LessonCategory.Reformer);
        var performance = Class(db, "Bootcamp", LessonCategory.Performance);
        var reformerSession = Session(db, reformer, Today.AddDays(1).AddHours(10), capacityOverride: 6);
        Session(db, performance, Today.AddDays(1).AddHours(11));
        var member = new MemberProfile { ApplicationUser = new() { UserName = "member" }, MembershipPackage = new() { Code = MembershipPackageCode.Start } };
        db.ClassReservations.Add(new() { ClassSession = reformerSession, MemberProfile = member });
        db.ClassReservations.Add(new() { ClassSession = reformerSession, MemberProfile = new() { ApplicationUser = new() { UserName = "other" }, MembershipPackage = member.MembershipPackage } });
        await db.SaveChangesAsync();
        var service = new MemberClassScheduleService(db);

        var all = await service.GetAsync(member.Id, new(), NowUtc);
        Assert.Equal(2, all.Sessions.Count);
        Assert.Equal([LessonCategory.Performance, LessonCategory.Reformer], all.Categories);
        Assert.All(all.Classes, option => Assert.Equal("Test Coach", option.TrainerName));

        var reformerOnly = await service.GetAsync(member.Id, new(Category: LessonCategory.Reformer), NowUtc);
        var session = Assert.Single(reformerOnly.Sessions);
        Assert.Equal(LessonCategory.Reformer, session.Category);
        Assert.Equal(6, session.Capacity);
        Assert.Equal(2, session.ReservedCount);
        Assert.True(session.IsReservedByMember);

        var byClass = await service.GetAsync(member.Id, new(ClassId: performance.Id), NowUtc);
        Assert.Equal("Bootcamp", Assert.Single(byClass.Sessions).ClassName);

        var mismatch = await service.GetAsync(member.Id, new(Category: LessonCategory.Reformer, ClassId: performance.Id), NowUtc);
        Assert.Null(mismatch.ClassId);
        Assert.Equal("Reformer Flow", Assert.Single(mismatch.Sessions).ClassName);
    }

    private static GroupClass Class(ApplicationDbContext db, string name, LessonCategory category)
    {
        var groupClass = new GroupClass
        {
            Name = name,
            CreditCategory = category,
            Trainer = new Trainer { FirstName = "Test", LastName = "Coach", Specialty = "Group" },
            DurationMinutes = 50,
            Capacity = 10,
            IsActive = true
        };
        db.GroupClasses.Add(groupClass);
        return groupClass;
    }

    private static ClassSession Session(ApplicationDbContext db, GroupClass groupClass, DateTime startsAtLocal, int? capacityOverride = null)
    {
        var session = new ClassSession
        {
            GroupClass = groupClass,
            StartsAtUtc = ClubTime.ToUtc(startsAtLocal),
            CapacityOverride = capacityOverride,
            Status = ClassSessionStatus.Scheduled
        };
        db.ClassSessions.Add(session);
        return session;
    }

    private static ApplicationDbContext Db() => new(
        new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);
}
