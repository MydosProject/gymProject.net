using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NO23.Web.Data;
using NO23.Web.Domain.Enums;
using NO23.Web.ViewModels.Classes;

namespace NO23.Web.Controllers;

public class ClassesController(ApplicationDbContext dbContext) : Controller
{
    private const string ReservationTargetUrl = "/Member/Home#available-classes";

    public async Task<IActionResult> Index()
    {
        var nowUtc = DateTime.UtcNow;

        var groupClasses = await dbContext.GroupClasses
            .AsNoTracking()
            .Where(groupClass => groupClass.IsActive)
            .OrderBy(groupClass => groupClass.Name)
            .Select(groupClass => new
            {
                GroupClassId = groupClass.Id,
                groupClass.Name,
                groupClass.Description,
                groupClass.DurationMinutes,
                DifficultyLevel = groupClass.DifficultyLevel.ToString(),
                groupClass.AverageCaloriesBurned,
                TrainerName = groupClass.Trainer.FirstName + " " + groupClass.Trainer.LastName
            })
            .ToListAsync();

        var upcomingSessions = await dbContext.ClassSessions
            .AsNoTracking()
            .Where(session =>
                session.Status == ClassSessionStatus.Scheduled &&
                session.StartsAtUtc >= nowUtc &&
                session.GroupClass.IsActive)
            .OrderBy(session => session.StartsAtUtc)
            .Select(session => new
            {
                ClassSessionId = session.Id,
                session.GroupClassId,
                ClassName = session.GroupClass.Name,
                TrainerName = session.GroupClass.Trainer.FirstName + " " + session.GroupClass.Trainer.LastName,
                session.StartsAtUtc,
                Capacity = session.CapacityOverride ?? session.GroupClass.Capacity,
                ReservedCount = session.Reservations.Count(reservation =>
                    reservation.Status == ClassReservationStatus.Reserved)
            })
            .ToListAsync();

        var sessionViewModels = upcomingSessions
            .Select(session => new UpcomingClassSessionPublicViewModel
            {
                ClassSessionId = session.ClassSessionId,
                GroupClassId = session.GroupClassId,
                ClassName = session.ClassName,
                TrainerName = session.TrainerName,
                StartsAtUtc = session.StartsAtUtc,
                StartsAtLocal = session.StartsAtUtc.ToLocalTime(),
                Capacity = session.Capacity,
                ReservedCount = session.ReservedCount,
                RemainingCapacity = Math.Max(0, session.Capacity - session.ReservedCount)
            })
            .ToList();

        var sessionsByGroupClass = sessionViewModels
            .GroupBy(session => session.GroupClassId)
            .ToDictionary(group => group.Key, group => (IReadOnlyList<UpcomingClassSessionPublicViewModel>)group.ToList());

        var groupClassViewModels = groupClasses
            .Select(groupClass => new GroupClassPublicViewModel
            {
                GroupClassId = groupClass.GroupClassId,
                Name = groupClass.Name,
                Description = groupClass.Description,
                DurationMinutes = groupClass.DurationMinutes,
                DifficultyLevel = groupClass.DifficultyLevel,
                AverageCaloriesBurned = groupClass.AverageCaloriesBurned,
                TrainerName = groupClass.TrainerName,
                UpcomingSessions = sessionsByGroupClass.GetValueOrDefault(groupClass.GroupClassId, [])
            })
            .ToList();

        return View(new ClassesIndexViewModel
        {
            GroupClasses = groupClassViewModels,
            UpcomingSessions = sessionViewModels,
            ReservationTargetUrl = ReservationTargetUrl
        });
    }
}
