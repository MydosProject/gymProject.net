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
        var model = new KitchenMenuItemFormViewModel
        {
            Calories = 450,
            UnitPrice = 250,
            ProteinGrams = 30,
            CarbohydrateGrams = 45,
            FatGrams = 15,
            DisplayOrder = 10,
            IsActive = true
        };
        await PopulateOptionsAsync(model, null);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(KitchenMenuItemFormViewModel model)
    {
        await ValidateAllergensAsync(model.SelectedAllergenIds);
        if (!ModelState.IsValid)
        {
            await PopulateOptionsAsync(model, null);
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
        await PopulateOptionsAsync(model, id);

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

        await ValidateAllergensAsync(model.SelectedAllergenIds);
        if (!ModelState.IsValid)
        {
            await PopulateOptionsAsync(model, id);
            return View(model);
        }

        var item = await dbContext.KitchenMenuItems
            .Include(item => item.RecipeIngredients)
            .Include(item => item.MenuItemAllergens)
            .FirstOrDefaultAsync(item => item.Id == id);

        if (item is null)
        {
            return NotFound();
        }

        ApplyFormModel(item, model);
        dbContext.KitchenRecipeIngredients.RemoveRange(item.RecipeIngredients);
        item.RecipeIngredients.Clear();
        ApplyRecipeIngredients(item, model);
        SyncAllergens(item, model);
        await dbContext.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var item = await dbContext.KitchenMenuItems
            .FirstOrDefaultAsync(item => item.Id == id);

        if (item is null)
        {
            return NotFound();
        }

        var isInUse =
            await dbContext.OrderItems.AnyAsync(orderItem =>
                orderItem.KitchenMenuItemId == id) ||
            await dbContext.CartItems.AnyAsync(cartItem =>
                cartItem.KitchenMenuItemId == id) ||
            await dbContext.KitchenMealPlanItems.AnyAsync(planItem =>
                planItem.KitchenMenuItemId == id) ||
            await dbContext.KitchenProductionPlanItems.AnyAsync(productionItem =>
                productionItem.KitchenMenuItemId == id);

        if (isInUse)
        {
            ModelState.AddModelError(
                string.Empty,
                "Bu ürün sipariş, sepet veya plan kayıtlarında kullanıldığı için silinemez. Bunun yerine ürünü pasif duruma getirebilirsin.");

            var model = MapToFormModel(item);
            await PopulateOptionsAsync(model, id);

            return View("Edit", model);
        }

        dbContext.KitchenMenuItems.Remove(item);
        await dbContext.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    private static KitchenMenuItem MapToEntity(KitchenMenuItemFormViewModel model)
    {
        var item = new KitchenMenuItem();
        ApplyFormModel(item, model);
        ApplyRecipeIngredients(item, model);
        ApplyAllergens(item, model);
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

    private static void ApplyAllergens(KitchenMenuItem item, KitchenMenuItemFormViewModel model)
    {
        foreach (var allergenId in model.SelectedAllergenIds.Distinct())
            item.MenuItemAllergens.Add(new KitchenMenuItemAllergen { KitchenAllergenId = allergenId });
    }

    private void SyncAllergens(KitchenMenuItem item, KitchenMenuItemFormViewModel model)
    {
        var selectedIds = model.SelectedAllergenIds.ToHashSet();
        var removedItems = item.MenuItemAllergens.Where(x => !selectedIds.Contains(x.KitchenAllergenId)).ToList();
        dbContext.KitchenMenuItemAllergens.RemoveRange(removedItems);
        foreach (var allergenId in selectedIds.Except(item.MenuItemAllergens.Select(x => x.KitchenAllergenId)))
            item.MenuItemAllergens.Add(new KitchenMenuItemAllergen { KitchenAllergenId = allergenId });
    }

    private async Task PopulateOptionsAsync(KitchenMenuItemFormViewModel model, int? menuItemId)
    {
        model.RecipeIngredients = await BuildRecipeInputsAsync(menuItemId, model.RecipeIngredients);
        var existingIds = model.SelectedAllergenIds.Count > 0
            ? model.SelectedAllergenIds.ToHashSet()
            : menuItemId.HasValue
                ? (await dbContext.KitchenMenuItemAllergens.AsNoTracking()
                    .Where(x => x.KitchenMenuItemId == menuItemId.Value)
                    .Select(x => x.KitchenAllergenId).ToListAsync()).ToHashSet()
                : [];
        model.SelectedAllergenIds = existingIds.ToList();
        model.AllergenOptions = await dbContext.KitchenAllergens.AsNoTracking()
            .Where(x => x.IsActive || existingIds.Contains(x.Id))
            .OrderBy(x => x.DisplayOrder).ThenBy(x => x.Name)
            .Select(x => new KitchenAllergenOptionViewModel { Id = x.Id, Name = x.Name, IsSelected = existingIds.Contains(x.Id) })
            .ToListAsync();
    }

    private async Task ValidateAllergensAsync(IReadOnlyCollection<int> allergenIds)
    {
        var distinctIds = allergenIds.Distinct().ToList();
        var validCount = await dbContext.KitchenAllergens.CountAsync(x => distinctIds.Contains(x.Id));
        if (validCount != distinctIds.Count)
            ModelState.AddModelError(nameof(KitchenMenuItemFormViewModel.SelectedAllergenIds), "Geçersiz bir alerjen seçildi.");
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
