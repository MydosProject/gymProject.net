using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NO23.Web.Data;
using NO23.Web.Data.Seed;
using NO23.Web.Domain.Enums;
using NO23.Web.Extensions;
using NO23.Web.ViewModels.TrainerPanel;

namespace NO23.Web.Areas.Trainer.Controllers;

[Area("Trainer")]
[Authorize(Roles = ApplicationRoles.Trainer)]
public class DashboardController(ApplicationDbContext dbContext)
    : Controller
{
    public async Task<IActionResult> Index()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Challenge();
        }

        var trainer = await dbContext.Trainers
            .AsNoTracking()
            .Where(item => item.ApplicationUserId == userId)
            .Select(item => new
            {
                item.Id,
                item.FirstName,
                item.LastName,
                item.Specialty
            })
            .FirstOrDefaultAsync();

        if (trainer is null)
        {
            return Forbid();
        }

        var nowUtc = DateTime.UtcNow;

        var pendingRequestCount =
            await dbContext.PersonalTrainingRequests
                .AsNoTracking()
                .CountAsync(request =>
                    request.TrainerId == trainer.Id &&
                    request.Status ==
                    PersonalTrainingRequestStatus.Pending);

        var upcomingPersonalTrainingCount =
            await dbContext.PersonalTrainingRequests
                .AsNoTracking()
                .CountAsync(request =>
                    request.TrainerId == trainer.Id &&
                    request.Status ==
                    PersonalTrainingRequestStatus.Scheduled &&
                    request.ScheduledAtUtc != null &&
                    request.ScheduledAtUtc >= nowUtc);

        var activeGroupClassCount =
            await dbContext.GroupClasses
                .AsNoTracking()
                .CountAsync(groupClass =>
                    groupClass.TrainerId == trainer.Id &&
                    groupClass.IsActive);

        var upcomingClassSessionCount =
            await dbContext.ClassSessions
                .AsNoTracking()
                .CountAsync(session =>
                    session.GroupClass.TrainerId == trainer.Id &&
                    session.GroupClass.IsActive &&
                    session.Status == ClassSessionStatus.Scheduled &&
                    session.StartsAtUtc >= nowUtc);

        var requestRows =
            await dbContext.PersonalTrainingRequests
                .AsNoTracking()
                .Where(request =>
                    request.TrainerId == trainer.Id)
                .OrderByDescending(request =>
                    request.Status ==
                    PersonalTrainingRequestStatus.Pending)
                .ThenByDescending(request =>
                    request.CreatedAtUtc)
                .Take(5)
                .Select(request => new
                {
                    request.Id,
                    MemberFirstName =
                        request.MemberProfile.ApplicationUser.FirstName,
                    MemberLastName =
                        request.MemberProfile.ApplicationUser.LastName,
                    MemberEmail =
                        request.MemberProfile.ApplicationUser.Email,
                    request.PreferredDate,
                    request.PreferredTimeWindow,
                    request.GoalNote,
                    request.Status,
                    request.ScheduledAtUtc,
                    request.CreatedAtUtc
                })
                .ToListAsync();

        var recentRequests = requestRows
            .Select(request =>
            {
                var memberName =
                    $"{request.MemberFirstName} {request.MemberLastName}"
                        .Trim();

                var memberEmail =
                    request.MemberEmail ?? string.Empty;

                return new
                    TrainerPersonalTrainingRequestListItemViewModel
                    {
                        Id = request.Id,
                        MemberName =
                            string.IsNullOrWhiteSpace(memberName)
                                ? memberEmail
                                : memberName,
                        MemberEmail = memberEmail,
                        PreferredDate = request.PreferredDate,
                        PreferredTimeWindow =
                            request.PreferredTimeWindow,
                        GoalNote = request.GoalNote,
                        Status = request.Status,
                        StatusDisplayName =
                            request.Status.GetDisplayName(),
                        ScheduledAtUtc =
                            request.ScheduledAtUtc,
                        CreatedAtUtc =
                            request.CreatedAtUtc
                    };
            })
            .ToList();

        var sessionRows =
            await dbContext.ClassSessions
                .AsNoTracking()
                .Where(session =>
                    session.GroupClass.TrainerId == trainer.Id &&
                    session.GroupClass.IsActive &&
                    session.Status ==
                    ClassSessionStatus.Scheduled &&
                    session.StartsAtUtc >= nowUtc)
                .OrderBy(session => session.StartsAtUtc)
                .Take(5)
                .Select(session => new
                {
                    session.Id,
                    ClassName = session.GroupClass.Name,
                    session.StartsAtUtc,
                    session.GroupClass.DurationMinutes,
                    Capacity =
                        session.CapacityOverride ??
                        session.GroupClass.Capacity,
                    ReservedCount =
                        session.Reservations.Count(reservation =>
                            reservation.Status ==
                            ClassReservationStatus.Reserved),
                    session.Status
                })
                .ToListAsync();

        var upcomingSessions = sessionRows
            .Select(session =>
                new TrainerClassSessionListItemViewModel
                {
                    SessionId = session.Id,
                    ClassName = session.ClassName,
                    StartsAtUtc = session.StartsAtUtc,
                    DurationMinutes =
                        session.DurationMinutes,
                    Capacity = session.Capacity,
                    ReservedCount =
                        session.ReservedCount,
                    StatusDisplayName =
                        session.Status.GetDisplayName()
                })
            .ToList();

        var trainerName =
            $"{trainer.FirstName} {trainer.LastName}".Trim();

        var model = new TrainerDashboardViewModel
        {
            TrainerName = trainerName,
            Specialty = trainer.Specialty,
            PendingRequestCount =
                pendingRequestCount,
            UpcomingPersonalTrainingCount =
                upcomingPersonalTrainingCount,
            ActiveGroupClassCount =
                activeGroupClassCount,
            UpcomingClassSessionCount =
                upcomingClassSessionCount,
            RecentPersonalTrainingRequests =
                recentRequests,
            UpcomingClassSessions =
                upcomingSessions
        };

        return View(model);
    }
}