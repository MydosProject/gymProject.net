using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NO23.Web.Controllers;
using NO23.Web.Data;
using NO23.Web.Domain.Enums;
using NO23.Web.ViewModels;

namespace NO23.Tests;

public class AppointmentRequestTests
{
    [Fact]
    public async Task GuestRequest_IsPendingAndDoesNotCreateMemberOrDuplicate()
    {
        await using var db = Db();
        var controller = new AppointmentsController(db);
        var input = Valid();
        Assert.IsType<RedirectToActionResult>(await controller.Index(input));
        await controller.Index(input);
        var request = await db.AppointmentRequests.SingleAsync();
        Assert.Equal(ServicePackageApplicationStatus.Pending, request.Status);
        Assert.Equal(input.PreferredDate, request.PreferredDate);
        Assert.Equal(input.PreferredTime, request.PreferredTime);
        Assert.Empty(db.MemberProfiles);
        Assert.Empty(db.PersonalTrainingSessions);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task PastDateOrUnknownService_DoesNotCreateRequest(bool past)
    {
        await using var db = Db();
        var input = Valid();
        if (past) input.PreferredDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1));
        else input.Service = "Geçersiz";
        var controller = new AppointmentsController(db);
        Assert.IsType<ViewResult>(await controller.Index(input));
        Assert.False(controller.ModelState.IsValid);
        Assert.Empty(db.AppointmentRequests);
    }

    private static AppointmentRequestViewModel Valid() => new()
    { FullName = "Test Ziyaretçi", PhoneNumber = "05555555555", PreferredDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(2)), PreferredTime = new TimeOnly(14, 30) };
    private static ApplicationDbContext Db() => new(new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
}
