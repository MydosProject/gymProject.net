using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using NO23.Web.Data;
using NO23.Web.Data.Seed;
using NO23.Web.Domain.Entities;
using NO23.Web.Extensions;
using NO23.Web.ViewModels.Admin;

namespace NO23.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = ApplicationRoles.Admin)]
public class GroupClassesController(ApplicationDbContext dbContext) : Controller
{
    public async Task<IActionResult> Index()
    {
        var classRows = await dbContext.GroupClasses
            .AsNoTracking()
            .Include(groupClass => groupClass.Trainer)
            .OrderBy(groupClass => groupClass.Name)
            .Select(groupClass => new
            {
                Id = groupClass.Id,
                Name = groupClass.Name,
                TrainerName = groupClass.Trainer.FirstName + " " + groupClass.Trainer.LastName,
                groupClass.DifficultyLevel,
                DurationMinutes = groupClass.DurationMinutes,
                AverageCaloriesBurned = groupClass.AverageCaloriesBurned,
                Capacity = groupClass.Capacity,
                SessionCount = groupClass.Sessions.Count,
                IsActive = groupClass.IsActive
            })
            .ToListAsync();

        var classes = classRows
            .Select(groupClass => new GroupClassListItemViewModel
            {
                Id = groupClass.Id,
                Name = groupClass.Name,
                TrainerName = groupClass.TrainerName,
                DifficultyLevel = groupClass.DifficultyLevel.GetDisplayName(),
                DurationMinutes = groupClass.DurationMinutes,
                AverageCaloriesBurned = groupClass.AverageCaloriesBurned,
                Capacity = groupClass.Capacity,
                SessionCount = groupClass.SessionCount,
                IsActive = groupClass.IsActive
            })
            .ToList();

        return View(classes);
    }

    public async Task<IActionResult> Create()
    {
        return View(await PopulateTrainerOptionsAsync(new GroupClassFormViewModel
        {
            DurationMinutes = 45,
            AverageCaloriesBurned = 350,
            Capacity = 12,
            IsActive = true
        }));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(GroupClassFormViewModel model)
    {
        if (!await TrainerExistsAsync(model.TrainerId))
        {
            ModelState.AddModelError(nameof(model.TrainerId), "Geçerli bir eğitmen seçmelisin.");
        }

        if (!ModelState.IsValid)
        {
            return View(await PopulateTrainerOptionsAsync(model));
        }

        dbContext.GroupClasses.Add(MapToEntity(model));
        await dbContext.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var groupClass = await dbContext.GroupClasses
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == id);

        if (groupClass is null)
        {
            return NotFound();
        }

        return View(await PopulateTrainerOptionsAsync(MapToFormModel(groupClass)));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, GroupClassFormViewModel model)
    {
        if (id != model.Id)
        {
            return BadRequest();
        }

        if (!await TrainerExistsAsync(model.TrainerId))
        {
            ModelState.AddModelError(nameof(model.TrainerId), "Geçerli bir eğitmen seçmelisin.");
        }

        if (!ModelState.IsValid)
        {
            return View(await PopulateTrainerOptionsAsync(model));
        }

        var groupClass = await dbContext.GroupClasses.FindAsync(id);

        if (groupClass is null)
        {
            return NotFound();
        }

        ApplyFormModel(groupClass, model);
        await dbContext.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    private async Task<GroupClassFormViewModel> PopulateTrainerOptionsAsync(GroupClassFormViewModel model)
    {
        model.TrainerOptions = await dbContext.Trainers
            .AsNoTracking()
            .Where(trainer => trainer.IsActive)
            .OrderBy(trainer => trainer.FirstName)
            .ThenBy(trainer => trainer.LastName)
            .Select(trainer => new SelectListItem(
                trainer.FirstName + " " + trainer.LastName + " - " + trainer.Specialty,
                trainer.Id.ToString()))
            .ToListAsync();

        return model;
    }

    private async Task<bool> TrainerExistsAsync(int trainerId)
    {
        return await dbContext.Trainers.AnyAsync(trainer => trainer.Id == trainerId && trainer.IsActive);
    }

    private static GroupClass MapToEntity(GroupClassFormViewModel model)
    {
        var groupClass = new GroupClass();
        ApplyFormModel(groupClass, model);
        return groupClass;
    }

    private static void ApplyFormModel(GroupClass groupClass, GroupClassFormViewModel model)
    {
        groupClass.TrainerId = model.TrainerId;
        groupClass.Name = model.Name.Trim();
        groupClass.Description = model.Description?.Trim();
        groupClass.DurationMinutes = model.DurationMinutes;
        groupClass.DifficultyLevel = model.DifficultyLevel;
        groupClass.AverageCaloriesBurned = model.AverageCaloriesBurned;
        groupClass.Capacity = model.Capacity;
        groupClass.IsActive = model.IsActive;
        groupClass.UpdatedAtUtc = DateTime.UtcNow;
    }

    private static GroupClassFormViewModel MapToFormModel(GroupClass groupClass)
    {
        return new GroupClassFormViewModel
        {
            Id = groupClass.Id,
            TrainerId = groupClass.TrainerId,
            Name = groupClass.Name,
            Description = groupClass.Description,
            DurationMinutes = groupClass.DurationMinutes,
            DifficultyLevel = groupClass.DifficultyLevel,
            AverageCaloriesBurned = groupClass.AverageCaloriesBurned,
            Capacity = groupClass.Capacity,
            IsActive = groupClass.IsActive
        };
    }
}
