using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NO23.Web.Data;
using NO23.Web.Data.Seed;
using NO23.Web.Domain.Entities;
using NO23.Web.ViewModels.Admin;

namespace NO23.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = ApplicationRoles.Admin)]
public class TrainersController(ApplicationDbContext dbContext) : Controller
{
    public async Task<IActionResult> Index()
    {
        var trainers = await dbContext.Trainers
            .AsNoTracking()
            .OrderBy(trainer => trainer.LastName)
            .ThenBy(trainer => trainer.FirstName)
            .Select(trainer => new TrainerListItemViewModel
            {
                Id = trainer.Id,
                FullName = trainer.FirstName + " " + trainer.LastName,
                Specialty = trainer.Specialty,
                Certifications = trainer.Certifications,
                ClassCount = trainer.GroupClasses.Count,
                IsActive = trainer.IsActive
            })
            .ToListAsync();

        return View(trainers);
    }

    public IActionResult Create()
    {
        return View(new TrainerFormViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(TrainerFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        dbContext.Trainers.Add(MapToEntity(model));
        await dbContext.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var trainer = await dbContext.Trainers
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == id);

        if (trainer is null)
        {
            return NotFound();
        }

        return View(MapToFormModel(trainer));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, TrainerFormViewModel model)
    {
        if (id != model.Id)
        {
            return BadRequest();
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var trainer = await dbContext.Trainers.FindAsync(id);

        if (trainer is null)
        {
            return NotFound();
        }

        ApplyFormModel(trainer, model);
        await dbContext.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    private static Trainer MapToEntity(TrainerFormViewModel model)
    {
        var trainer = new Trainer();
        ApplyFormModel(trainer, model);
        return trainer;
    }

    private static void ApplyFormModel(Trainer trainer, TrainerFormViewModel model)
    {
        trainer.FirstName = model.FirstName.Trim();
        trainer.LastName = model.LastName.Trim();
        trainer.Specialty = model.Specialty.Trim();
        trainer.Certifications = model.Certifications?.Trim();
        trainer.Bio = model.Bio?.Trim();
        trainer.IsActive = model.IsActive;
        trainer.UpdatedAtUtc = DateTime.UtcNow;
    }

    private static TrainerFormViewModel MapToFormModel(Trainer trainer)
    {
        return new TrainerFormViewModel
        {
            Id = trainer.Id,
            FirstName = trainer.FirstName,
            LastName = trainer.LastName,
            Specialty = trainer.Specialty,
            Certifications = trainer.Certifications,
            Bio = trainer.Bio,
            IsActive = trainer.IsActive
        };
    }
}
