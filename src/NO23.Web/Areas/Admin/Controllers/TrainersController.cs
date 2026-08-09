using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NO23.Web.Data;
using NO23.Web.Data.Seed;
using NO23.Web.Domain.Entities;
using NO23.Web.ViewModels.Admin;
using Microsoft.AspNetCore.Identity;
using TrainerEntity = NO23.Web.Domain.Entities.Trainer;

namespace NO23.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = ApplicationRoles.Admin)]
public class TrainersController(
    ApplicationDbContext dbContext,
    UserManager<ApplicationUser> userManager) : Controller
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
                IsActive = trainer.IsActive,
                HasPanelAccount = trainer.ApplicationUserId != null
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

    [HttpGet]
public async Task<IActionResult> CreatePanelAccount(int id)
{
    var trainer = await dbContext.Trainers
        .AsNoTracking()
        .FirstOrDefaultAsync(item => item.Id == id);

    if (trainer is null)
    {
        return NotFound();
    }

    if (!string.IsNullOrWhiteSpace(trainer.ApplicationUserId))
    {
        TempData["StatusMessage"] =
            "Bu eğitmenin zaten bir panel hesabı var.";

        return RedirectToAction(nameof(Index));
    }

    return View(new TrainerPanelAccountViewModel
    {
        TrainerId = trainer.Id,
        TrainerName = trainer.FirstName + " " + trainer.LastName
    });
}

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreatePanelAccount(
        TrainerPanelAccountViewModel model)
    {
        var trainer = await dbContext.Trainers
            .FirstOrDefaultAsync(item => item.Id == model.TrainerId);

        if (trainer is null)
        {
            return NotFound();
        }

        model.TrainerName =
            trainer.FirstName + " " + trainer.LastName;

        if (!string.IsNullOrWhiteSpace(trainer.ApplicationUserId))
        {
            ModelState.AddModelError(
                string.Empty,
                "Bu eğitmenin zaten bir panel hesabı var.");
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var email = model.Email.Trim();

        var existingUser =
            await userManager.FindByEmailAsync(email);

        if (existingUser is not null)
        {
            ModelState.AddModelError(
                nameof(model.Email),
                "Bu e-posta adresi zaten kullanılıyor.");

            return View(model);
        }

        await using var transaction =
            await dbContext.Database.BeginTransactionAsync();

        var applicationUser = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            FirstName = trainer.FirstName,
            LastName = trainer.LastName
        };

        var createResult =
            await userManager.CreateAsync(
                applicationUser,
                model.Password);

        if (!createResult.Succeeded)
        {
            AddIdentityErrors(createResult);
            return View(model);
        }

        var roleResult =
            await userManager.AddToRoleAsync(
                applicationUser,
                ApplicationRoles.Trainer);

        if (!roleResult.Succeeded)
        {
            AddIdentityErrors(roleResult);
            return View(model);
        }

        trainer.ApplicationUserId = applicationUser.Id;
        trainer.UpdatedAtUtc = DateTime.UtcNow;

        await dbContext.SaveChangesAsync();
        await transaction.CommitAsync();

        TempData["StatusMessage"] =
            $"{model.TrainerName} için trainer panel hesabı oluşturuldu.";

        return RedirectToAction(nameof(Index));
    }

    private void AddIdentityErrors(IdentityResult result)
    {
        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(
                string.Empty,
                error.Description);
        }
    }

    private static TrainerEntity MapToEntity(TrainerFormViewModel model)
    {
        var trainer = new TrainerEntity();

        ApplyFormModel(trainer, model);

        return trainer;
    }

    private static void ApplyFormModel(
        TrainerEntity trainer,
        TrainerFormViewModel model)
    {
        trainer.FirstName = model.FirstName.Trim();
        trainer.LastName = model.LastName.Trim();
        trainer.Specialty = model.Specialty.Trim();
        trainer.Certifications = model.Certifications?.Trim();
        trainer.Bio = model.Bio?.Trim();
        trainer.IsActive = model.IsActive;
        trainer.UpdatedAtUtc = DateTime.UtcNow;
    }

    private static TrainerFormViewModel MapToFormModel(
        TrainerEntity trainer)
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
