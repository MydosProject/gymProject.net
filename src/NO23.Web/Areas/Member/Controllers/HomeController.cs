using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NO23.Web.Data;
using NO23.Web.Data.Seed;
using NO23.Web.Domain.Enums;
using NO23.Web.Services;
using NO23.Web.ViewModels.Member;

namespace NO23.Web.Areas.Member.Controllers;

[Area("Member")]
[Authorize(Roles = ApplicationRoles.Member)]
public class HomeController(
    ApplicationDbContext dbContext,
    ClassReservationService reservationService) : Controller
{
    public async Task<IActionResult> Index()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Challenge();
        }

        var profile = await dbContext.MemberProfiles
            .AsNoTracking()
            .Include(member => member.ApplicationUser)
            .Include(member => member.MembershipPackage)
            .FirstOrDefaultAsync(member => member.ApplicationUserId == userId);

        if (profile is null)
        {
            return View(new MemberDashboardViewModel());
        }

        var upcomingReservations = await dbContext.ClassReservations
            .AsNoTracking()
            .Include(reservation => reservation.ClassSession)
            .ThenInclude(session => session.GroupClass)
            .ThenInclude(groupClass => groupClass.Trainer)
            .Where(reservation =>
                reservation.MemberProfileId == profile.Id &&
                reservation.Status == ClassReservationStatus.Reserved &&
                reservation.ClassSession.StartsAtUtc >= DateTime.UtcNow)
            .OrderBy(reservation => reservation.ClassSession.StartsAtUtc)
            .Select(reservation => new MemberReservationViewModel
            {
                ReservationId = reservation.Id,
                ClassName = reservation.ClassSession.GroupClass.Name,
                TrainerName = reservation.ClassSession.GroupClass.Trainer.FirstName + " " + reservation.ClassSession.GroupClass.Trainer.LastName,
                StartsAtUtc = reservation.ClassSession.StartsAtUtc
            })
            .ToListAsync();

        var availableSessions = await dbContext.ClassSessions
            .AsNoTracking()
            .Include(session => session.GroupClass)
            .ThenInclude(groupClass => groupClass.Trainer)
            .Include(session => session.Reservations)
            .Where(session =>
                session.Status == ClassSessionStatus.Scheduled &&
                session.StartsAtUtc >= DateTime.UtcNow &&
                session.GroupClass.IsActive)
            .OrderBy(session => session.StartsAtUtc)
            .Take(20)
            .Select(session => new AvailableClassSessionViewModel
            {
                SessionId = session.Id,
                ClassName = session.GroupClass.Name,
                TrainerName = session.GroupClass.Trainer.FirstName + " " + session.GroupClass.Trainer.LastName,
                StartsAtUtc = session.StartsAtUtc,
                DurationMinutes = session.GroupClass.DurationMinutes,
                DifficultyLevel = session.GroupClass.DifficultyLevel.ToString(),
                AverageCaloriesBurned = session.GroupClass.AverageCaloriesBurned,
                Capacity = session.CapacityOverride ?? session.GroupClass.Capacity,
                ReservedCount = session.Reservations.Count(reservation => reservation.Status == ClassReservationStatus.Reserved),
                IsReservedByMember = session.Reservations.Any(reservation =>
                    reservation.MemberProfileId == profile.Id &&
                    reservation.Status == ClassReservationStatus.Reserved)
            })
            .ToListAsync();

        var memberName = (profile.ApplicationUser.FirstName + " " + profile.ApplicationUser.LastName).Trim();
        var today = DateOnly.FromDateTime(DateTime.Today);
        var hasActiveKitchenSubscription = await dbContext.KitchenSubscriptions
            .AsNoTracking()
            .AnyAsync(subscription =>
                subscription.MemberProfileId == profile.Id &&
                subscription.Status == KitchenSubscriptionStatus.Active &&
                subscription.EndsOn >= today);

        return View(new MemberDashboardViewModel
        {
            MemberName = string.IsNullOrWhiteSpace(memberName) ? profile.ApplicationUser.Email ?? "NO23 Member" : memberName,
            PackageName = profile.MembershipPackage.Name,
            RemainingClassCredits = profile.RemainingClassCredits,
            HasUnlimitedClasses = profile.MembershipPackage.WeeklyClassLimit is null,
            HasActiveKitchenSubscription = hasActiveKitchenSubscription,
            UpcomingReservations = upcomingReservations,
            AvailableSessions = availableSessions
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reserve(int classSessionId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Challenge();
        }

        var result = await reservationService.ReserveAsync(userId, classSessionId);
        TempData[result.Succeeded ? "SuccessMessage" : "ErrorMessage"] =
            result.Succeeded ? "Rezervasyon oluşturuldu." : result.ErrorMessage;

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int reservationId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Challenge();
        }

        var result = await reservationService.CancelAsync(userId, reservationId);
        TempData[result.Succeeded ? "SuccessMessage" : "ErrorMessage"] =
            result.Succeeded ? "Rezervasyon iptal edildi." : result.ErrorMessage;

        return RedirectToAction(nameof(Index));
    }
}
