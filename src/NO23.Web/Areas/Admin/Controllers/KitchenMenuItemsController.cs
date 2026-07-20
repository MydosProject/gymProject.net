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
public class KitchenMenuItemsController(ApplicationDbContext dbContext) : Controller
{
    public async Task<IActionResult> Index()
    {
        var items = await dbContext.KitchenMenuItems
            .AsNoTracking()
            .OrderBy(item => item.DisplayOrder)
            .ThenBy(item => item.Name)
            .Select(item => new KitchenMenuItemListItemViewModel
            {
                Id = item.Id,
                Name = item.Name,
                Category = item.Category.ToString(),
                Calories = item.Calories,
                UnitPrice = item.UnitPrice,
                ProteinGrams = item.ProteinGrams,
                CarbohydrateGrams = item.CarbohydrateGrams,
                FatGrams = item.FatGrams,
                Tags = item.Tags,
                IsActive = item.IsActive,
                DisplayOrder = item.DisplayOrder
            })
            .ToListAsync();

        return View(items);
    }

    public IActionResult Create()
    {
        return View(new KitchenMenuItemFormViewModel
        {
            Calories = 450,
            UnitPrice = 250,
            ProteinGrams = 30,
            CarbohydrateGrams = 45,
            FatGrams = 15,
            DisplayOrder = 10,
            IsActive = true
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(KitchenMenuItemFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        dbContext.KitchenMenuItems.Add(MapToEntity(model));
        await dbContext.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var item = await dbContext.KitchenMenuItems
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == id);

        if (item is null)
        {
            return NotFound();
        }

        return View(MapToFormModel(item));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, KitchenMenuItemFormViewModel model)
    {
        if (id != model.Id)
        {
            return BadRequest();
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var item = await dbContext.KitchenMenuItems.FindAsync(id);

        if (item is null)
        {
            return NotFound();
        }

        ApplyFormModel(item, model);
        await dbContext.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    private static KitchenMenuItem MapToEntity(KitchenMenuItemFormViewModel model)
    {
        var item = new KitchenMenuItem();
        ApplyFormModel(item, model);
        return item;
    }

    private static void ApplyFormModel(KitchenMenuItem item, KitchenMenuItemFormViewModel model)
    {
        item.Name = model.Name.Trim();
        item.Description = model.Description?.Trim();
        item.Category = model.Category;
        item.Calories = model.Calories;
        item.UnitPrice = model.UnitPrice;
        item.ProteinGrams = model.ProteinGrams;
        item.CarbohydrateGrams = model.CarbohydrateGrams;
        item.FatGrams = model.FatGrams;
        item.Ingredients = model.Ingredients.Trim();
        item.Allergens = model.Allergens?.Trim();
        item.Tags = model.Tags?.Trim();
        item.IsActive = model.IsActive;
        item.DisplayOrder = model.DisplayOrder;
        item.UpdatedAtUtc = DateTime.UtcNow;
    }

    private static KitchenMenuItemFormViewModel MapToFormModel(KitchenMenuItem item)
    {
        return new KitchenMenuItemFormViewModel
        {
            Id = item.Id,
            Name = item.Name,
            Description = item.Description,
            Category = item.Category,
            Calories = item.Calories,
            UnitPrice = item.UnitPrice,
            ProteinGrams = item.ProteinGrams,
            CarbohydrateGrams = item.CarbohydrateGrams,
            FatGrams = item.FatGrams,
            Ingredients = item.Ingredients,
            Allergens = item.Allergens,
            Tags = item.Tags,
            IsActive = item.IsActive,
            DisplayOrder = item.DisplayOrder
        };
    }
}
