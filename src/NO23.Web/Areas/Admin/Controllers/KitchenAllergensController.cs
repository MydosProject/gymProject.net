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
public class KitchenAllergensController(ApplicationDbContext dbContext) : Controller
{
    public async Task<IActionResult> Index()
    {
        var items = await dbContext.KitchenAllergens.AsNoTracking()
            .OrderBy(item => item.DisplayOrder).ThenBy(item => item.Name)
            .Select(item => new KitchenAllergenListItemViewModel
            {
                Id = item.Id, Name = item.Name, Description = item.Description,
                IsActive = item.IsActive, DisplayOrder = item.DisplayOrder,
                MenuItemCount = item.MenuItems.Count, MemberCount = item.Members.Count
            }).ToListAsync();
        return View(items);
    }

    [HttpGet]
    public IActionResult Create() => View(new KitchenAllergenFormViewModel());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(KitchenAllergenFormViewModel model)
    {
        await ValidateNameAsync(model.Name, null);
        if (!ModelState.IsValid) return View(model);
        dbContext.KitchenAllergens.Add(new KitchenAllergen
        {
            Name = model.Name.Trim(), Description = model.Description?.Trim(),
            IsActive = model.IsActive, DisplayOrder = model.DisplayOrder
        });
        await dbContext.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var item = await dbContext.KitchenAllergens.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        if (item is null) return NotFound();
        return View(new KitchenAllergenFormViewModel
        {
            Id = item.Id, Name = item.Name, Description = item.Description,
            IsActive = item.IsActive, DisplayOrder = item.DisplayOrder
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, KitchenAllergenFormViewModel model)
    {
        if (id != model.Id) return BadRequest();
        await ValidateNameAsync(model.Name, id);
        if (!ModelState.IsValid) return View(model);
        var item = await dbContext.KitchenAllergens.FirstOrDefaultAsync(x => x.Id == id);
        if (item is null) return NotFound();
        item.Name = model.Name.Trim(); item.Description = model.Description?.Trim();
        item.IsActive = model.IsActive; item.DisplayOrder = model.DisplayOrder;
        item.UpdatedAtUtc = DateTime.UtcNow;
        await dbContext.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var item = await dbContext.KitchenAllergens.Include(x => x.MenuItems).Include(x => x.Members)
            .FirstOrDefaultAsync(x => x.Id == id);
        if (item is null) return NotFound();
        if (item.MenuItems.Count > 0 || item.Members.Count > 0)
        {
            TempData["ErrorMessage"] = "Kullanımda olan alerjen silinemez; pasif duruma getirebilirsin.";
            return RedirectToAction(nameof(Index));
        }
        dbContext.KitchenAllergens.Remove(item);
        await dbContext.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    private async Task ValidateNameAsync(string? name, int? id)
    {
        if (string.IsNullOrWhiteSpace(name)) return;
        var normalized = name.Trim().ToLower();
        if (await dbContext.KitchenAllergens.AnyAsync(x => (!id.HasValue || x.Id != id.Value) && x.Name.ToLower() == normalized))
            ModelState.AddModelError(nameof(KitchenAllergenFormViewModel.Name), "Bu alerjen zaten tanımlı.");
    }
}
