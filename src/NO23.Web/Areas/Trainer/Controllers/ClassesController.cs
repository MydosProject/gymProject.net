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
public class ClassesController(ApplicationDbContext dbContext)
    : Controller
{
    public async Task<IActionResult> Index()
    {
        var userId =
            User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Challenge();
        }

        var trainerId =
            await dbContext.Trainers
                .AsNoTracking()
                .Where(trainer =>
                    trainer.ApplicationUserId == userId)
                .Select(trainer => (int?)trainer.Id)
                .FirstOrDefaultAsync();

        if (trainerId is null)
        {
            return Forbid();
        }

        var nowUtc = DateTime.UtcNow;

        var rows = await dbContext.GroupClasses
            .AsNoTracking()
            .Where(groupClass =>
                groupClass.TrainerId == trainerId.Value)
            .OrderByDescending(groupClass =>
                groupClass.IsActive)
            .ThenBy(groupClass => groupClass.Name)
            .Select(groupClass => new
            {
                groupClass.Id,
                groupClass.Name,
                groupClass.DifficultyLevel,
                groupClass.DurationMinutes,
                groupClass.AverageCaloriesBurned,
                groupClass.Capacity,
                groupClass.IsActive,

                UpcomingSessionCount =
                    groupClass.Sessions.Count(session =>
                        session.Status ==
                        ClassSessionStatus.Scheduled &&
                        session.StartsAtUtc >= nowUtc),

                NextSessionAtUtc =
                    groupClass.Sessions
                        .Where(session =>
                            session.Status ==
                            ClassSessionStatus.Scheduled &&
                            session.StartsAtUtc >= nowUtc)
                        .OrderBy(session =>
                            session.StartsAtUtc)
                        .Select(session =>
                            (DateTime?)session.StartsAtUtc)
                        .FirstOrDefault()
            })
            .ToListAsync();

        var model = rows
            .Select(groupClass =>
                new TrainerGroupClassListItemViewModel
                {
                    Id = groupClass.Id,
                    Name = groupClass.Name,
                    DifficultyLevel =
                        groupClass.DifficultyLevel
                            .GetDisplayName(),
                    DurationMinutes =
                        groupClass.DurationMinutes,
                    AverageCaloriesBurned =
                        groupClass.AverageCaloriesBurned,
                    Capacity = groupClass.Capacity,
                    IsActive = groupClass.IsActive,
                    UpcomingSessionCount =
                        groupClass.UpcomingSessionCount,
                    NextSessionAtUtc =
                        groupClass.NextSessionAtUtc
                })
            .ToList();

        return View(model);
    }
}