using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NO23.Web.Data;
using NO23.Web.Data.Seed;
using NO23.Web.Domain.Entities;
using NO23.Web.Domain.Enums;
using NO23.Web.Services;
using NO23.Web.ViewModels.Admin;

namespace NO23.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = ApplicationRoles.Admin)]
public class KitchenStockController(
    ApplicationDbContext dbContext,
    KitchenProductionPlanningService productionPlanningService) : Controller
{
    public async Task<IActionResult> Index(DateOnly? date)
    {
        var selectedDate = date ?? DateOnly.FromDateTime(DateTime.Today);

        return View(await BuildDashboardAsync(
            selectedDate,
            new KitchenIngredientFormViewModel(),
            new KitchenStockMovementFormViewModel()));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> GeneratePlan(DateOnly selectedDate)
    {
        var result = await productionPlanningService.CreateOrRefreshPlanAsync(selectedDate);
        TempData[result.Succeeded ? "SuccessMessage" : "ErrorMessage"] =
            result.Succeeded
                ? "Üretim planı güncellendi."
                : result.Message;

        return RedirectToAction(nameof(Index), new { date = selectedDate.ToString("yyyy-MM-dd") });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdatePlanStatus(
        int planId,
        KitchenProductionPlanStatus status,
        DateOnly selectedDate)
    {
        var result = await productionPlanningService.UpdatePlanStatusAsync(planId, status);
        TempData[result.Succeeded ? "SuccessMessage" : "ErrorMessage"] =
            result.Succeeded
                ? status == KitchenProductionPlanStatus.Completed
                    ? "Üretim planı tamamlandı ve stok düşümü işlendi."
                    : "Üretim planı durumu güncellendi."
                : result.Message;

        return RedirectToAction(nameof(Index), new { date = selectedDate.ToString("yyyy-MM-dd") });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateItemStatus(
        int itemId,
        KitchenProductionItemStatus status,
        DateOnly selectedDate)
    {
        var result = await productionPlanningService.UpdateItemStatusAsync(itemId, status);
        TempData[result.Succeeded ? "SuccessMessage" : "ErrorMessage"] =
            result.Succeeded
                ? "Ürün hazırlık durumu güncellendi."
                : result.Message;

        return RedirectToAction(nameof(Index), new { date = selectedDate.ToString("yyyy-MM-dd") });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateIngredient(
        KitchenIngredientFormViewModel model,
        DateOnly selectedDate)
    {
        if (await IngredientNameExistsAsync(model))
        {
            ModelState.AddModelError(nameof(model.Name), "Bu malzeme adı zaten tanımlı.");
        }

        if (!ModelState.IsValid)
        {
            return View("Index", await BuildDashboardAsync(
                selectedDate,
                model,
                new KitchenStockMovementFormViewModel()));
        }

        dbContext.KitchenIngredients.Add(MapToEntity(model));
        await dbContext.SaveChangesAsync();

        TempData["SuccessMessage"] = "Malzeme kaydedildi.";

        return RedirectToAction(nameof(Index), new { date = selectedDate.ToString("yyyy-MM-dd") });
    }

    public async Task<IActionResult> EditIngredient(int id, DateOnly? date)
    {
        var ingredient = await dbContext.KitchenIngredients
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == id);

        if (ingredient is null)
        {
            return NotFound();
        }

        ViewData["SelectedDate"] = date?.ToString("yyyy-MM-dd") ?? DateOnly
            .FromDateTime(DateTime.Today)
            .ToString("yyyy-MM-dd");

        return View(MapToFormModel(ingredient));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditIngredient(
        int id,
        KitchenIngredientFormViewModel model,
        DateOnly selectedDate)
    {
        if (id != model.Id)
        {
            return BadRequest();
        }

        if (await IngredientNameExistsAsync(model))
        {
            ModelState.AddModelError(nameof(model.Name), "Bu malzeme adı zaten tanımlı.");
        }

        if (!ModelState.IsValid)
        {
            ViewData["SelectedDate"] = selectedDate.ToString("yyyy-MM-dd");
            return View(model);
        }

        var ingredient = await dbContext.KitchenIngredients.FindAsync(id);

        if (ingredient is null)
        {
            return NotFound();
        }

        ApplyFormModel(ingredient, model);
        await dbContext.SaveChangesAsync();

        TempData["SuccessMessage"] = "Malzeme güncellendi.";

        return RedirectToAction(nameof(Index), new { date = selectedDate.ToString("yyyy-MM-dd") });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateStockMovement(
        KitchenStockMovementFormViewModel model,
        DateOnly selectedDate)
    {
        if (!ModelState.IsValid)
        {
            return View("Index", await BuildDashboardAsync(
                selectedDate,
                new KitchenIngredientFormViewModel(),
                model));
        }

        var result = await productionPlanningService.CreateStockMovementAsync(
            model.KitchenIngredientId,
            model.Type,
            model.Quantity,
            model.Note);

        TempData[result.Succeeded ? "SuccessMessage" : "ErrorMessage"] =
            result.Succeeded
                ? "Stok hareketi kaydedildi."
                : result.Message;

        return RedirectToAction(nameof(Index), new { date = selectedDate.ToString("yyyy-MM-dd") });
    }

    private async Task<KitchenStockDashboardViewModel> BuildDashboardAsync(
        DateOnly selectedDate,
        KitchenIngredientFormViewModel ingredientForm,
        KitchenStockMovementFormViewModel stockMovementForm)
    {
        var productionPlan = await dbContext.KitchenProductionPlans
            .AsNoTracking()
            .Include(plan => plan.Items)
            .Include(plan => plan.Materials)
                .ThenInclude(material => material.KitchenIngredient)
            .FirstOrDefaultAsync(plan => plan.PlanDate == selectedDate);

        var productionMenuItemIds = productionPlan?.Items
            .Select(item => item.KitchenMenuItemId)
            .Distinct()
            .ToList()
            ?? [];

        List<KitchenStockRecipeRow> recipeRows = productionMenuItemIds.Count == 0
            ? []
            : await dbContext.KitchenRecipeIngredients
                .AsNoTracking()
                .Where(recipe => productionMenuItemIds.Contains(recipe.KitchenMenuItemId))
                .Select(recipe => new KitchenStockRecipeRow(
                    recipe.KitchenMenuItemId,
                    recipe.KitchenIngredient.Name,
                    recipe.KitchenIngredient.Unit,
                    recipe.QuantityPerPortion,
                    recipe.KitchenIngredient.CurrentStockQuantity))
                .ToListAsync();

        var recipeRowsByMenuItemId = recipeRows
            .GroupBy(recipe => recipe.KitchenMenuItemId)
            .ToDictionary(group => group.Key, group => group.ToList());

        var rawIngredients = await dbContext.KitchenIngredients
            .AsNoTracking()
            .OrderBy(ingredient => ingredient.Name)
            .ToListAsync();

        var ingredients = rawIngredients
            .Select(ingredient => new KitchenIngredientListItemViewModel
            {
                Id = ingredient.Id,
                Name = ingredient.Name,
                Unit = GetIngredientUnitDisplayName(ingredient.Unit),
                CurrentStockQuantity = ingredient.CurrentStockQuantity,
                MinimumStockQuantity = ingredient.MinimumStockQuantity,
                IsActive = ingredient.IsActive
            })
            .ToList();

        var rawMovements = await dbContext.KitchenStockMovements
            .AsNoTracking()
            .Include(movement => movement.KitchenIngredient)
            .OrderByDescending(movement => movement.CreatedAtUtc)
            .Take(10)
            .ToListAsync();

        var movements = rawMovements
            .Select(movement => new KitchenStockMovementListItemViewModel
            {
                IngredientName = movement.KitchenIngredient.Name,
                Type = GetStockMovementTypeDisplayName(movement.Type),
                Quantity = movement.Quantity,
                Unit = GetIngredientUnitDisplayName(movement.KitchenIngredient.Unit),
                QuantityBefore = movement.QuantityBeforeSnapshot,
                QuantityAfter = movement.QuantityAfterSnapshot,
                Note = movement.Note,
                CreatedAtUtc = movement.CreatedAtUtc
            })
            .ToList();

        return new KitchenStockDashboardViewModel
        {
            SelectedDate = selectedDate,
            ProductionPlan = productionPlan is null
                ? null
                : MapProductionPlan(productionPlan, recipeRowsByMenuItemId),
            Ingredients = ingredients,
            RecentMovements = movements,
            IngredientForm = ingredientForm,
            StockMovementForm = stockMovementForm
        };
    }

    private static KitchenProductionPlanViewModel MapProductionPlan(
        KitchenProductionPlan plan,
        IReadOnlyDictionary<int, List<KitchenStockRecipeRow>> recipeRowsByMenuItemId)
    {
        return new KitchenProductionPlanViewModel
        {
            Id = plan.Id,
            PlanDate = plan.PlanDate,
            StockDeductedAtUtc = plan.StockDeductedAtUtc,
            Status = plan.Status.ToString(),
            StatusDisplayName = GetProductionPlanStatusDisplayName(plan.Status),
            IsCompleted = plan.Status == KitchenProductionPlanStatus.Completed,
            Items = plan.Items
                .OrderByDescending(item => item.TotalPortions)
                .ThenBy(item => item.ProductNameSnapshot)
                .Select(item => new KitchenProductionPlanItemViewModel
                {
                    Id = item.Id,
                    ProductName = item.ProductNameSnapshot,
                    SubscriptionPortions = item.SubscriptionPortions,
                    OrderPortions = item.OrderPortions,
                    TotalPortions = item.TotalPortions,
                    HasRecipe = item.HasRecipeSnapshot,
                    Status = item.Status.ToString(),
                    StatusDisplayName = GetProductionItemStatusDisplayName(item.Status),
                    RecipeIngredients = recipeRowsByMenuItemId.TryGetValue(
                            item.KitchenMenuItemId,
                            out var recipeRows)
                        ? recipeRows
                            .OrderBy(recipe => recipe.IngredientName)
                            .Select(recipe => new KitchenProductionPlanRecipeIngredientViewModel
                            {
                                IngredientName = recipe.IngredientName,
                                Unit = GetIngredientUnitDisplayName(recipe.Unit),
                                QuantityPerPortion = recipe.QuantityPerPortion,
                                RequiredQuantity = recipe.QuantityPerPortion * item.TotalPortions,
                                CurrentStockQuantity = recipe.CurrentStockQuantity
                            })
                            .ToList()
                        : []
                })
                .ToList(),
            Materials = plan.Materials
                .Select(material =>
                {
                    var currentStockQuantity = material.KitchenIngredient.CurrentStockQuantity;
                    var minimumStockQuantity = material.KitchenIngredient.MinimumStockQuantity;

                    return new KitchenProductionPlanMaterialViewModel
                    {
                        IngredientName = material.IngredientNameSnapshot,
                        Unit = GetIngredientUnitDisplayName(material.UnitSnapshot),
                        RequiredQuantity = material.RequiredQuantity,
                        StockQuantity = currentStockQuantity,
                        MinimumStockQuantity = minimumStockQuantity,
                        MissingQuantity = KitchenProductionPlanCalculator.CalculateProductionMissingQuantity(
                            material.RequiredQuantity,
                            currentStockQuantity),
                        SuggestedStockEntryQuantity =
                            KitchenProductionPlanCalculator.CalculateSuggestedStockEntryQuantity(
                                material.RequiredQuantity,
                                currentStockQuantity,
                                minimumStockQuantity)
                    };
                })
                .OrderByDescending(material => material.HasMissingStock)
                .ThenByDescending(material => material.HasSuggestedStockEntry)
                .ThenByDescending(material => material.SuggestedStockEntryQuantity)
                .ThenBy(material => material.IngredientName)
                .ToList()
        };
    }

    private async Task<bool> IngredientNameExistsAsync(KitchenIngredientFormViewModel model)
    {
        var normalizedName = (model.Name ?? string.Empty).Trim();

        return await dbContext.KitchenIngredients
            .AnyAsync(ingredient => ingredient.Name == normalizedName && ingredient.Id != model.Id);
    }

    private static KitchenIngredient MapToEntity(KitchenIngredientFormViewModel model)
    {
        var ingredient = new KitchenIngredient();
        ApplyFormModel(ingredient, model);
        return ingredient;
    }

    private static void ApplyFormModel(
        KitchenIngredient ingredient,
        KitchenIngredientFormViewModel model)
    {
        ingredient.Name = (model.Name ?? string.Empty).Trim();
        ingredient.Unit = model.Unit;
        ingredient.CurrentStockQuantity = model.CurrentStockQuantity;
        ingredient.MinimumStockQuantity = model.MinimumStockQuantity;
        ingredient.IsActive = model.IsActive;
        ingredient.UpdatedAtUtc = DateTime.UtcNow;
    }

    private static KitchenIngredientFormViewModel MapToFormModel(KitchenIngredient ingredient)
    {
        return new KitchenIngredientFormViewModel
        {
            Id = ingredient.Id,
            Name = ingredient.Name,
            Unit = ingredient.Unit,
            CurrentStockQuantity = ingredient.CurrentStockQuantity,
            MinimumStockQuantity = ingredient.MinimumStockQuantity,
            IsActive = ingredient.IsActive
        };
    }

    public static string GetIngredientUnitDisplayName(KitchenIngredientUnit unit)
    {
        return unit switch
        {
            KitchenIngredientUnit.Gram => "gr",
            KitchenIngredientUnit.Milliliter => "ml",
            KitchenIngredientUnit.Piece => "adet",
            _ => unit.ToString()
        };
    }

    public static string GetProductionPlanStatusDisplayName(KitchenProductionPlanStatus status)
    {
        return status switch
        {
            KitchenProductionPlanStatus.Draft => "Taslak",
            KitchenProductionPlanStatus.InPreparation => "Hazırlanıyor",
            KitchenProductionPlanStatus.Completed => "Tamamlandı",
            KitchenProductionPlanStatus.Cancelled => "İptal Edildi",
            _ => status.ToString()
        };
    }

    public static string GetProductionItemStatusDisplayName(KitchenProductionItemStatus status)
    {
        return status switch
        {
            KitchenProductionItemStatus.NotStarted => "Başlamadı",
            KitchenProductionItemStatus.Preparing => "Hazırlanıyor",
            KitchenProductionItemStatus.Ready => "Hazır",
            _ => status.ToString()
        };
    }

    public static string GetStockMovementTypeDisplayName(KitchenStockMovementType type)
    {
        return type switch
        {
            KitchenStockMovementType.StockIn => "Stok Girişi",
            KitchenStockMovementType.StockOut => "Stok Çıkışı",
            KitchenStockMovementType.Adjustment => "Sayım Düzeltmesi",
            _ => type.ToString()
        };
    }
}

public record KitchenStockRecipeRow(
    int KitchenMenuItemId,
    string IngredientName,
    KitchenIngredientUnit Unit,
    decimal QuantityPerPortion,
    decimal CurrentStockQuantity);
