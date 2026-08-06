using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NO23.Web.Data;
using NO23.Web.Data.Seed;
using NO23.Web.Domain.Enums;
using NO23.Web.ViewModels.Admin;

namespace NO23.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = ApplicationRoles.Admin)]
public class DashboardController(ApplicationDbContext dbContext)
    : Controller
{
    public async Task<IActionResult> Index()
    {
        var todayStartLocal = DateTime.Today;
        var tomorrowStartLocal = todayStartLocal.AddDays(1);

        var todayStartUtc = todayStartLocal.ToUniversalTime();
        var tomorrowStartUtc = tomorrowStartLocal.ToUniversalTime();

        var nowUtc = DateTime.UtcNow;

        var model = new AdminDashboardViewModel
        {
            TotalMembers = await dbContext.MemberProfiles
                .AsNoTracking()
                .CountAsync(),

            ActiveTrainers = await dbContext.Trainers
                .AsNoTracking()
                .CountAsync(trainer => trainer.IsActive),

            TodayClassSessions = await dbContext.ClassSessions
                .AsNoTracking()
                .CountAsync(session =>
                    session.Status == ClassSessionStatus.Scheduled &&
                    session.StartsAtUtc >= todayStartUtc &&
                    session.StartsAtUtc < tomorrowStartUtc),

            UpcomingCommunityEvents = await dbContext.CommunityEvents
                .AsNoTracking()
                .CountAsync(item =>
                    item.Status == CommunityEventStatus.Scheduled &&
                    item.StartsAtUtc >= nowUtc),

            PendingPersonalTrainingRequests =
                await dbContext.PersonalTrainingRequests
                    .AsNoTracking()
                    .CountAsync(request =>
                        request.Status ==
                        PersonalTrainingRequestStatus.Pending),

            PendingOrders = await dbContext.Orders
                .AsNoTracking()
                .CountAsync(order =>
                    order.Status == OrderStatus.Pending ||
                    order.Status == OrderStatus.Confirmed ||
                    order.Status == OrderStatus.Preparing)
        };

        return View(model);
    }
}