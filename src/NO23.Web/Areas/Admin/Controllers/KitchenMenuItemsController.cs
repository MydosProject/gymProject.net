using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NO23.Web.Data;
using NO23.Web.Data.Seed;
using NO23.Web.Domain.Entities;
using NO23.Web.Domain.Enums;
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

    public async Task<IActionResult> Create()
    {
        return View(new KitchenMenuItemFormViewModel
        {
            Calories = 450,
            UnitPrice = 250,
            ProteinGrams = 30,
            CarbohydrateGrams = 45,
            FatGrams = 15,
            DisplayOrder = 10,
            IsActive = true,
            RecipeIngredients = await BuildRecipeInputsAsync(null)
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(KitchenMenuItemFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            model.RecipeIngredients = await BuildRecipeInputsAsync(null, model.RecipeIngredients);
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
            .Include(item => item.RecipeIngredients)
            .FirstOrDefaultAsync(item => item.Id == id);

        if (item is null)
        {
            return NotFound();
        }

        var model = MapToFormModel(item);
        model.RecipeIngredients = await BuildRecipeInputsAsync(id);

        return View(model);
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
            model.RecipeIngredients = await BuildRecipeInputsAsync(id, model.RecipeIngredients);
            return View(model);
        }

        var item = await dbContext.KitchenMenuItems
            .Include(item => item.RecipeIngredients)
            .FirstOrDefaultAsync(item => item.Id == id);

        if (item is null)
        {
            return NotFound();
        }

        ApplyFormModel(item, model);
        dbContext.KitchenRecipeIngredients.RemoveRange(item.RecipeIngredients);
        item.RecipeIngredients.Clear();
        ApplyRecipeIngredients(item, model);
        await dbContext.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    private static KitchenMenuItem MapToEntity(KitchenMenuItemFormViewModel model)
    {
        var item = new KitchenMenuItem();
        ApplyFormModel(item, model);
        ApplyRecipeIngredients(item, model);
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

    private async Task<List<KitchenMenuItemRecipeIngredientInputViewModel>> BuildRecipeInputsAsync(
        int? kitchenMenuItemId,
        IReadOnlyList<KitchenMenuItemRecipeIngredientInputViewModel>? postedInputs = null)
    {
        var postedQuantityByIngredientId = postedInputs?
            .ToDictionary(input => input.KitchenIngredientId, input => input.QuantityPerPortion)
            ?? [];

        Dictionary<int, decimal> existingQuantityByIngredientId = [];

        if (kitchenMenuItemId is not null)
        {
            existingQuantityByIngredientId = await dbContext.KitchenRecipeIngredients
                .AsNoTracking()
                .Where(recipe => recipe.KitchenMenuItemId == kitchenMenuItemId.Value)
                .ToDictionaryAsync(
                    recipe => recipe.KitchenIngredientId,
                    recipe => recipe.QuantityPerPortion);
        }

        var ingredients = await dbContext.KitchenIngredients
            .AsNoTracking()
            .Where(ingredient => ingredient.IsActive)
            .OrderBy(ingredient => ingredient.Name)
            .ToListAsync();

        return ingredients
            .Select(ingredient => new KitchenMenuItemRecipeIngredientInputViewModel
            {
                KitchenIngredientId = ingredient.Id,
                IngredientName = ingredient.Name,
                Unit = ingredient.Unit,
                UnitDisplayName = GetIngredientUnitDisplayName(ingredient.Unit),
                QuantityPerPortion = postedQuantityByIngredientId.TryGetValue(ingredient.Id, out var postedQuantity)
                    ? postedQuantity
                    : existingQuantityByIngredientId.TryGetValue(ingredient.Id, out var existingQuantity)
                        ? existingQuantity
                        : 0
            })
            .ToList();
    }

    private static void ApplyRecipeIngredients(
        KitchenMenuItem item,
        KitchenMenuItemFormViewModel model)
    {
        foreach (var recipeInput in model.RecipeIngredients.Where(input => input.QuantityPerPortion > 0))
        {
            item.RecipeIngredients.Add(new KitchenRecipeIngredient
            {
                KitchenIngredientId = recipeInput.KitchenIngredientId,
                QuantityPerPortion = recipeInput.QuantityPerPortion
            });
        }
    }

    private static string GetIngredientUnitDisplayName(KitchenIngredientUnit unit)
    {
        return unit switch
        {
            KitchenIngredientUnit.Gram => "gr",
            KitchenIngredientUnit.Milliliter => "ml",
            KitchenIngredientUnit.Piece => "adet",
            _ => unit.ToString()
        };
    }
}
