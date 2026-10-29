using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NO23.Web.Areas.Admin.Controllers;
using NO23.Web.Data;
using NO23.Web.Domain.Entities;
using NO23.Web.Domain.Enums;
using NO23.Web.ViewModels;
using NO23.Web.ViewModels.Admin;

namespace NO23.Tests;

public class AdminListNavigationTests
{
    [Theory]
    [InlineData(1, 25)]
    [InlineData(2, 12)]
    [InlineData(int.MaxValue, 12)]
    [InlineData(-1, 25)]
    public async Task Members_PagesAreBoundedWhileOverallMetricsRemainGlobal(int page, int expected)
    {
        await using var db = await Members();
        var controller = new MembersController(db, null!, TestMembershipModule.CreateDirectory(db));
        var result = Assert.IsType<ViewResult>(await controller.Index(page));
        var rows = Assert.IsAssignableFrom<IReadOnlyList<MemberListItemViewModel>>(result.Model);
        Assert.Equal(expected, rows.Count);
        Assert.Equal(37, controller.ViewData["OverallCount"]);
        Assert.Equal(1, controller.ViewData["PackageCount"]);
        Assert.Equal(rows.Count, rows.Select(x => x.MemberProfileId).Distinct().Count());
    }

    [Fact]
    public async Task Members_SearchKeepsSameNamedPeopleDistinctAndCombinesState()
    {
        await using var db = await Members();
        var controller = new MembersController(db, null!, TestMembershipModule.CreateDirectory(db));
        var rows = Assert.IsAssignableFrom<IReadOnlyList<MemberListItemViewModel>>(
            Assert.IsType<ViewResult>(await controller.Index(query: "İPEK AYNI")).Model);
        Assert.Equal(2, rows.Count);
        Assert.Equal(2, rows.Select(x => x.Email).Distinct().Count());
        rows = Assert.IsAssignableFrom<IReadOnlyList<MemberListItemViewModel>>(
            Assert.IsType<ViewResult>(await controller.Index(query: "İPEK AYNI", state: "expired")).Model);
        Assert.Single(rows);
        Assert.IsType<BadRequestResult>(await controller.Index(state: "invalid"));
    }

    [Fact]
    public async Task Requests_PageBoundariesPreservePendingFirstAndSearch()
    {
        await using var db = Db();
        for (var i = 0; i < 35; i++) db.AppointmentRequests.Add(new AppointmentRequest
        { FullName = $"Talep {i:00}", PhoneNumber = "05300000000", Status = i < 5 ? ServicePackageApplicationStatus.Approved : ServicePackageApplicationStatus.Pending });
        await db.SaveChangesAsync();
        var controller = new AppointmentRequestsController(db);
        var first = Assert.IsAssignableFrom<IReadOnlyList<AppointmentRequest>>(
            Assert.IsType<ViewResult>(await controller.Index()).Model);
        Assert.Equal(25, first.Count);
        Assert.All(first, x => Assert.Equal(ServicePackageApplicationStatus.Pending, x.Status));
        var last = Assert.IsAssignableFrom<IReadOnlyList<AppointmentRequest>>(
            Assert.IsType<ViewResult>(await controller.Index(2)).Model);
        Assert.Equal(10, last.Count);
        Assert.Empty(first.Select(x => x.Id).Intersect(last.Select(x => x.Id)));
        var filtered = Assert.IsAssignableFrom<IReadOnlyList<AppointmentRequest>>(
            Assert.IsType<ViewResult>(await controller.Index(query: "Talep 03", state: "Approved")).Model);
        Assert.Equal("Talep 03", Assert.Single(filtered).FullName);
        Assert.Equal(1, Assert.IsType<ListNavigationViewModel>(controller.ViewData["ListNavigation"]).TotalCount);
    }

    private static ApplicationDbContext Db() => new(new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
    [Fact]
    public async Task MemberDirectory_MonthlyMetricUsesIstanbulMonthBoundary()
    {
        await using var db = await Members();
        foreach (var member in db.MemberProfiles) member.CreatedAtUtc = new DateTime(2026, 10, 15, 0, 0, 0, DateTimeKind.Utc);
        var rows = await db.MemberProfiles.OrderBy(x => x.Id).Take(2).ToListAsync();
        rows[0].CreatedAtUtc = new DateTime(2026, 10, 31, 20, 59, 0, DateTimeKind.Utc);
        rows[1].CreatedAtUtc = new DateTime(2026, 10, 31, 21, 1, 0, DateTimeKind.Utc);
        await db.SaveChangesAsync();
        var clock = new NO23.Web.Services.ClubClock(new FixedTimeProvider(new DateTimeOffset(2026, 10, 31, 21, 30, 0, TimeSpan.Zero)));
        var page = await TestMembershipModule.CreateDirectory(db, clock).LoadAsync(1, null, null);
        Assert.Equal(1, page.NewThisMonthCount);
        Assert.Equal(37, page.OverallCount);
    }

    private static async Task<ApplicationDbContext> Members()
    {
        var db = Db();
        var package = new MembershipPackage { Name = "Pro", Code = MembershipPackageCode.Pro, WeeklyClassLimit = 4 };
        for (var i = 0; i < 37; i++) db.MemberProfiles.Add(new MemberProfile
        {
            ApplicationUser = new ApplicationUser { UserName = $"member{i}", FirstName = i < 2 ? "İpek" : "Üye", LastName = i < 2 ? "Aynı" : i.ToString(), Email = $"member{i}@example.test" },
            MembershipPackage = package, RemainingClassCredits = 10,
            MembershipEndsAtUtc = i == 0 ? DateTime.UtcNow.AddDays(-1) : DateTime.UtcNow.AddDays(30)
        });
        await db.SaveChangesAsync(); return db;
    }
}
