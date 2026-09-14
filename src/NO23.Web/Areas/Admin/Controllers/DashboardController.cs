using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NO23.Web.Data;
using NO23.Web.Data.Seed;
using NO23.Web.Domain.Enums;
using NO23.Web.Extensions;
using NO23.Web.Services;
using NO23.Web.ViewModels.Admin;

namespace NO23.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = ApplicationRoles.Admin)]
public class DashboardController(ApplicationDbContext dbContext)
    : Controller
{
    public async Task<IActionResult> Index()
    {
        var todayStartLocal = ClubTime.Now.Date;
        var tomorrowStartLocal = todayStartLocal.AddDays(1);

        var todayStartUtc = ClubTime.ToUtc(todayStartLocal);
        var tomorrowStartUtc = ClubTime.ToUtc(tomorrowStartLocal);

        var nowUtc = DateTime.UtcNow;
        var monthStartUtc = ClubTime.ToUtc(new DateTime(todayStartLocal.Year, todayStartLocal.Month, 1));
        var weekStartUtc = ClubTime.ToUtc(todayStartLocal.AddDays(-(((int)todayStartLocal.DayOfWeek + 6) % 7)));

        var requestRows = await dbContext.PersonalTrainingRequests
            .AsNoTracking()
            .OrderByDescending(request =>
                request.Status == PersonalTrainingRequestStatus.Pending)
            .ThenByDescending(request => request.CreatedAtUtc)
            .Take(6)
            .Select(request => new
            {
                request.Id,
                MemberName = ((request.MemberProfile.ApplicationUser.FirstName ?? "") + " " +
                              (request.MemberProfile.ApplicationUser.LastName ?? "")).Trim(),
                MemberEmail = request.MemberProfile.ApplicationUser.Email ?? "",
                TrainerName = (request.Trainer.FirstName + " " + request.Trainer.LastName).Trim(),
                request.PreferredDate,
                request.PreferredTimeWindow,
                request.Status,
                request.ScheduledAtUtc,
                request.CreatedAtUtc
            })
            .ToListAsync();

        var recentRequests = requestRows.Select(request => new PersonalTrainingRequestListItemViewModel
        {
            Id = request.Id,
            MemberName = string.IsNullOrWhiteSpace(request.MemberName) ? request.MemberEmail : request.MemberName,
            MemberEmail = request.MemberEmail,
            TrainerName = request.TrainerName,
            PreferredDate = request.PreferredDate,
            PreferredTimeWindow = request.PreferredTimeWindow,
            Status = request.Status.GetDisplayName(),
            IsPending = request.Status == PersonalTrainingRequestStatus.Pending,
            ScheduledAtUtc = request.ScheduledAtUtc,
            CreatedAtUtc = request.CreatedAtUtc
        }).ToList();

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
                    order.Status == OrderStatus.Preparing),

            RecentPersonalTrainingRequests = recentRequests
        };

        var activeTrainerRows = await dbContext.Trainers.AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.FirstName).ThenBy(x => x.LastName)
            .Select(x => new { x.Id, Name = (x.FirstName + " " + x.LastName).Trim() })
            .ToListAsync();
        var groupCounts = await dbContext.ClassSessions.AsNoTracking()
            .Where(x => x.Status == ClassSessionStatus.Completed && x.StartsAtUtc >= monthStartUtc && x.StartsAtUtc <= nowUtc)
            .GroupBy(x => x.GroupClass.TrainerId)
            .Select(x => new { TrainerId = x.Key, Count = x.Count() }).ToListAsync();
        var personalCounts = await dbContext.PersonalTrainingSessions.AsNoTracking()
            .Where(x => x.Status == PersonalTrainingSessionStatus.Completed && x.StartsAtUtc >= monthStartUtc && x.StartsAtUtc <= nowUtc)
            .GroupBy(x => x.TrainerId)
            .Select(x => new { TrainerId = x.Key, Count = x.Count() }).ToListAsync();
        var weeklyGroupCounts = await dbContext.ClassSessions.AsNoTracking()
            .Where(x => x.Status == ClassSessionStatus.Completed && x.StartsAtUtc >= weekStartUtc && x.StartsAtUtc <= nowUtc)
            .GroupBy(x => x.GroupClass.TrainerId).Select(x => new { TrainerId = x.Key, Count = x.Count() }).ToListAsync();
        var weeklyPersonalCounts = await dbContext.PersonalTrainingSessions.AsNoTracking()
            .Where(x => x.Status == PersonalTrainingSessionStatus.Completed && x.StartsAtUtc >= weekStartUtc && x.StartsAtUtc <= nowUtc)
            .GroupBy(x => x.TrainerId).Select(x => new { TrainerId = x.Key, Count = x.Count() }).ToListAsync();
        var weeklyCounts = weeklyGroupCounts.Concat(weeklyPersonalCounts).GroupBy(x => x.TrainerId)
            .ToDictionary(x => x.Key, x => x.Sum(y => y.Count));
        var monthlyCounts = groupCounts.Concat(personalCounts).GroupBy(x => x.TrainerId)
            .ToDictionary(x => x.Key, x => x.Sum(y => y.Count));
        model.MonthlyTrainerLessons = activeTrainerRows
            .Select(x => new TrainerMonthlyLessonViewModel { TrainerName = x.Name,
                CompletedLessonCountThisWeek = weeklyCounts.GetValueOrDefault(x.Id),
                CompletedLessonCount = monthlyCounts.GetValueOrDefault(x.Id) })
            .OrderByDescending(x => x.CompletedLessonCount).ToList();

        return View(model);
    }
}
