using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NO23.Web.Data;
using NO23.Web.ViewModels.Trainers;

namespace NO23.Web.Controllers;

public class TrainersController(ApplicationDbContext dbContext) : Controller
{
    public async Task<IActionResult> Index()
    {
        var trainers = await dbContext.Trainers
            .AsNoTracking()
            .Where(trainer => trainer.IsActive)
            .OrderBy(trainer => trainer.FirstName)
            .ThenBy(trainer => trainer.LastName)
            .Select(trainer => new TrainerCardViewModel
            {
                Id = trainer.Id,
                FullName = trainer.FirstName + " " + trainer.LastName,
                Specialty = trainer.Specialty,
                Certifications = trainer.Certifications,
                Bio = trainer.Bio,
                Classes = trainer.GroupClasses
                    .Where(groupClass => groupClass.IsActive)
                    .OrderBy(groupClass => groupClass.Name)
                    .Select(groupClass => groupClass.Name)
                    .ToList()
            })
            .ToListAsync();

        return View(trainers);
    }
}
