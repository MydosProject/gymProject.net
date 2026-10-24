using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NO23.Web.Data;
using NO23.Web.Domain.Entities;
using NO23.Web.Domain.Enums;
using NO23.Web.ViewModels.Admin;

namespace NO23.Tests;

public class DashboardPendingRequestTests
{
    [Fact]
    public async Task Dashboard_ShowsGuestAndPackageRequestsWithoutMembership_LimitsRowsButNotCounts()
    {
        await using var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var now = DateTime.UtcNow;
        for (var i = 0; i < 8; i++) db.AppointmentRequests.Add(new() { FullName = "Ziyaretçi " + i, Service = "Ön görüşme", CreatedAtUtc = now.AddMinutes(-i) });
        db.AppointmentRequests.Add(new() { FullName = "Kapalı", Status = ServicePackageApplicationStatus.Closed });
        db.AppointmentRequests.Add(new() { FullName = "Planlandı", Status = ServicePackageApplicationStatus.Approved, ScheduleStatus = AppointmentScheduleStatus.Scheduled });
        var package = new ServicePackage { Name = "FLEX", Slug = "flex", Category = ServicePackageCategory.PersonalTraining };
        var variant = new ServicePackageVariant { ServicePackage = package, Name = "10 Ders" };
        db.ServicePackageApplications.Add(new() { ServicePackage = package, ServicePackageVariant = variant, FullName = "Paket Ziyaretçi", CreatedAtUtc = now.AddMinutes(1) });
        db.ServicePackageApplications.Add(new() { ServicePackage = package, ServicePackageVariant = variant, FullName = "İşlenmiş", Status = ServicePackageApplicationStatus.Contacted });
        await db.SaveChangesAsync();
        var model = Assert.IsType<AdminDashboardViewModel>(Assert.IsType<ViewResult>(await new NO23.Web.Areas.Admin.Controllers.DashboardController(db).Index()).Model);
        Assert.Equal(8, model.PendingAppointmentRequests);
        Assert.Equal(1, model.PendingPackageApplications);
        Assert.Equal(9, model.TotalPendingRequests);
        Assert.Equal(6, model.PendingRequests.Count);
        Assert.Equal("Paket Ziyaretçi", model.PendingRequests[0].Name);
        Assert.Contains(model.PendingRequests, x => x.Controller == "AppointmentRequests");
        Assert.DoesNotContain(model.PendingRequests, x => x.Name is "Kapalı" or "Planlandı" or "İşlenmiş");
        Assert.Empty(db.MemberProfiles);
    }
}
