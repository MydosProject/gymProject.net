using Microsoft.EntityFrameworkCore;
using NO23.Web.Data;
using NO23.Web.Domain.Entities;
using NO23.Web.Domain.Enums;

namespace NO23.Web.Services;

public class KitchenProductionPlanningService
(ApplicationDbContext dbContext,
AdminStockNotificationService? adminStockNotificationService = null)
{
    public async Task<KitchenProductionPlanResult> CreateOrRefreshPlanAsync(DateOnly planDate)
    {
        var existingPlan = await dbContext.KitchenProductionPlans
            .Include(plan => plan.Items)
            .Include(plan => plan.Materials)
            .FirstOrDefaultAsync(plan => plan.PlanDate == planDate);

        if (existingPlan?.Status == KitchenProductionPlanStatus.Completed)
        {
            return KitchenProductionPlanResult.Fail(
                "Tamamlanmış üretim planı yeniden oluşturulamaz.");
        }

        var demandRows = await BuildDemandRowsAsync(planDate);
        var recipeRows = await BuildRecipeRowsAsync(
            demandRows.Select(row => row.KitchenMenuItemId).ToList());
        var draft = KitchenProductionPlanCalculator.Calculate(demandRows, recipeRows);

        var itemStatusByMenuItemId = existingPlan?.Items
            .ToDictionary(item => item.KitchenMenuItemId, item => item.Status)
            ?? [];

        var plan = existingPlan ?? new KitchenProductionPlan
        {
            PlanDate = planDate
        };

        if (existingPlan is null)
        {
            dbContext.KitchenProductionPlans.Add(plan);
        }
        else
        {
            dbContext.KitchenProductionPlanItems.RemoveRange(existingPlan.Items);
            dbContext.KitchenProductionPlanMaterials.RemoveRange(existingPlan.Materials);
            plan.UpdatedAtUtc = DateTime.UtcNow;
        }

        plan.Status = plan.Status == KitchenProductionPlanStatus.InPreparation
            ? KitchenProductionPlanStatus.InPreparation
            : KitchenProductionPlanStatus.Draft;

        plan.Items = draft.Items
            .Select(item => new KitchenProductionPlanItem
            {
                KitchenMenuItemId = item.KitchenMenuItemId,
                ProductNameSnapshot = item.ProductName,
                SubscriptionPortions = item.SubscriptionPortions,
                OrderPortions = item.OrderPortions,
                TotalPortions = item.TotalPortions,
                HasRecipeSnapshot = item.HasRecipe,
                Status = itemStatusByMenuItemId.TryGetValue(item.KitchenMenuItemId, out var status)
                    ? status
                    : KitchenProductionItemStatus.NotStarted
            })
            .ToList();

        plan.Materials = draft.Materials
            .Select(material => new KitchenProductionPlanMaterial
            {
                KitchenIngredientId = material.KitchenIngredientId,
                IngredientNameSnapshot = material.IngredientName,
                UnitSnapshot = material.Unit,
                RequiredQuantity = material.RequiredQuantity,
                StockQuantitySnapshot = material.StockQuantity,
                MissingQuantity = material.MissingQuantity
            })
            .ToList();

        await dbContext.SaveChangesAsync();

        return KitchenProductionPlanResult.Ok(plan.Id);
    }

    public async Task<KitchenProductionPlanResult> UpdatePlanStatusAsync(
        int planId,
        KitchenProductionPlanStatus status)
    {
        if (status == KitchenProductionPlanStatus.Completed)
        {
            return await CompletePlanAsync(planId);
        }

        var plan = await dbContext.KitchenProductionPlans.FindAsync(planId);

        if (plan is null)
        {
            return KitchenProductionPlanResult.Fail("Üretim planı bulunamadı.");
        }

        if (plan.Status == KitchenProductionPlanStatus.Completed &&
            status != KitchenProductionPlanStatus.Completed)
        {
            return KitchenProductionPlanResult.Fail(
                "Tamamlanmış üretim planı durumu değiştirilemez.");
        }

        plan.Status = status;
        plan.UpdatedAtUtc = DateTime.UtcNow;
        await dbContext.SaveChangesAsync();

        return KitchenProductionPlanResult.Ok(plan.Id);
    }

    private async Task<KitchenProductionPlanResult> CompletePlanAsync(int planId)
    {
        var plan = await dbContext.KitchenProductionPlans
            .Include(plan => plan.Items)
            .Include(plan => plan.Materials)
            .ThenInclude(material => material.KitchenIngredient)
            .FirstOrDefaultAsync(plan => plan.Id == planId);

        if (plan is null)
        {
            return KitchenProductionPlanResult.Fail("Üretim planı bulunamadı.");
        }

        if (plan.StockDeductedAtUtc is not null)
        {
            return KitchenProductionPlanResult.Ok(plan.Id);
        }

        if (plan.Status == KitchenProductionPlanStatus.Completed)
        {
            return KitchenProductionPlanResult.Fail(
                "Tamamlanmış üretim planı yeniden tamamlanamaz.");
        }

        var notReadyProducts = plan.Items
            .Where(item =>
                item.TotalPortions > 0 &&
                item.Status != KitchenProductionItemStatus.Ready)
            .Select(item => item.ProductNameSnapshot)
            .OrderBy(name => name)
            .ToList();

        if (notReadyProducts.Count > 0)
        {
            return KitchenProductionPlanResult.Fail(
                $"Hazır olmayan ürünler var: {string.Join(", ", notReadyProducts)}. Planı tamamlamadan önce tüm ürünleri hazır yapmalısın.");
        }

        var missingRecipeProducts = plan.Items
            .Where(item => item.TotalPortions > 0 && !item.HasRecipeSnapshot)
            .Select(item => item.ProductNameSnapshot)
            .OrderBy(name => name)
            .ToList();

        if (missingRecipeProducts.Count > 0)
        {
            return KitchenProductionPlanResult.Fail(
                $"Reçetesi eksik ürünler var: {string.Join(", ", missingRecipeProducts)}.");
        }

        var missingStockMessages = plan.Materials
            .Where(material =>
                KitchenProductionPlanCalculator.CalculateProductionMissingQuantity(
                    material.RequiredQuantity,
                    material.KitchenIngredient.CurrentStockQuantity) > 0)
            .Select(material =>
            {
                var missingQuantity = KitchenProductionPlanCalculator.CalculateProductionMissingQuantity(
                    material.RequiredQuantity,
                    material.KitchenIngredient.CurrentStockQuantity);

                return $"{material.IngredientNameSnapshot} için {FormatQuantity(missingQuantity)} {GetIngredientUnitDisplayName(material.UnitSnapshot)} eksik";
            })
            .ToList();

        if (missingStockMessages.Count > 0)
        {
            return KitchenProductionPlanResult.Fail(
                $"Stok yetersiz: {string.Join("; ", missingStockMessages)}.");
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync();
        var deductedAtUtc = DateTime.UtcNow;

        var stockChanges = new List<(
        int IngredientId,
        string IngredientName,
        decimal PreviousQuantity,
        decimal CurrentQuantity,
        decimal MinimumQuantity,
        string UnitText)>();

        foreach (var material in plan.Materials.Where(material => material.RequiredQuantity > 0))
        {
            var ingredient = material.KitchenIngredient;
            var quantityBefore = ingredient.CurrentStockQuantity;
            var quantityAfter = quantityBefore - material.RequiredQuantity;

            if (quantityAfter < 0)
            {
                await transaction.RollbackAsync();
                return KitchenProductionPlanResult.Fail(
                    $"{material.IngredientNameSnapshot} stoğu sıfırın altına düşemez.");
            }

            ingredient.CurrentStockQuantity = quantityAfter;
            ingredient.UpdatedAtUtc = deductedAtUtc;
            stockChanges.Add(
            (
                ingredient.Id,
                ingredient.Name,
                quantityBefore,
                quantityAfter,
                ingredient.MinimumStockQuantity,
                GetIngredientUnitDisplayName(ingredient.Unit)
            ));
            dbContext.KitchenStockMovements.Add(new KitchenStockMovement
            {
                KitchenIngredientId = ingredient.Id,
                KitchenProductionPlanId = plan.Id,
                Type = KitchenStockMovementType.StockOut,
                Quantity = material.RequiredQuantity,
                QuantityBeforeSnapshot = quantityBefore,
                QuantityAfterSnapshot = quantityAfter,
                Note = $"{plan.PlanDate:dd.MM.yyyy} üretim planı otomatik stok düşümü",
                CreatedAtUtc = deductedAtUtc
            });
        }

        plan.Status = KitchenProductionPlanStatus.Completed;
        plan.StockDeductedAtUtc = deductedAtUtc;
        plan.UpdatedAtUtc = deductedAtUtc;

        await dbContext.SaveChangesAsync();
        await transaction.CommitAsync();

        if (adminStockNotificationService is not null)
            {
                foreach (var stockChange in stockChanges)
                {
                    await adminStockNotificationService.PublishKitchenStockChangedAsync(
                        stockChange.IngredientId,
                        stockChange.IngredientName,
                        stockChange.PreviousQuantity,
                        stockChange.CurrentQuantity,
                        stockChange.MinimumQuantity,
                        stockChange.UnitText);
                }
            }

        return KitchenProductionPlanResult.Ok(plan.Id);
    }

    public async Task<KitchenProductionPlanResult> UpdateItemStatusAsync(
        int itemId,
        KitchenProductionItemStatus status)
    {
        var item = await dbContext.KitchenProductionPlanItems
            .Include(planItem => planItem.KitchenProductionPlan)
            .FirstOrDefaultAsync(planItem => planItem.Id == itemId);

        if (item is null)
        {
            return KitchenProductionPlanResult.Fail("Üretim kalemi bulunamadı.");
        }

        if (item.KitchenProductionPlan.Status == KitchenProductionPlanStatus.Completed)
        {
            return KitchenProductionPlanResult.Fail(
                "Tamamlanmış üretim planındaki ürün durumu değiştirilemez.");
        }

        item.Status = status;
        item.UpdatedAtUtc = DateTime.UtcNow;
        item.KitchenProductionPlan.Status = status == KitchenProductionItemStatus.NotStarted
            ? item.KitchenProductionPlan.Status
            : KitchenProductionPlanStatus.InPreparation;
        item.KitchenProductionPlan.UpdatedAtUtc = DateTime.UtcNow;

        await dbContext.SaveChangesAsync();

        return KitchenProductionPlanResult.Ok(item.KitchenProductionPlanId);
    }

    public async Task<KitchenProductionPlanResult> CreateStockMovementAsync(
        int ingredientId,
        KitchenStockMovementType type,
        decimal quantity,
        string? note)
    {
        if (quantity < 0)
        {
            return KitchenProductionPlanResult.Fail("Miktar sıfırdan küçük olamaz.");
        }

        var ingredient = await dbContext.KitchenIngredients.FindAsync(ingredientId);

        if (ingredient is null)
        {
            return KitchenProductionPlanResult.Fail("Malzeme bulunamadı.");
        }

        var quantityBefore = ingredient.CurrentStockQuantity;
        var quantityAfter = type switch
        {
            KitchenStockMovementType.StockIn => quantityBefore + quantity,
            KitchenStockMovementType.StockOut => quantityBefore - quantity,
            KitchenStockMovementType.Adjustment => quantity,
            _ => quantityBefore
        };

        if (quantityAfter < 0)
        {
            return KitchenProductionPlanResult.Fail(
                "Stok miktarı sıfırın altına düşemez.");
        }

        ingredient.CurrentStockQuantity = quantityAfter;
        ingredient.UpdatedAtUtc = DateTime.UtcNow;
        ingredient.StockMovements.Add(new KitchenStockMovement
        {
            Type = type,
            Quantity = quantity,
            QuantityBeforeSnapshot = quantityBefore,
            QuantityAfterSnapshot = quantityAfter,
            Note = note?.Trim()
        });

        await dbContext.SaveChangesAsync();

        if (adminStockNotificationService is not null)
            {
                await adminStockNotificationService.PublishKitchenStockChangedAsync(
                    ingredient.Id,
                    ingredient.Name,
                    quantityBefore,
                    quantityAfter,
                    ingredient.MinimumStockQuantity,
                    GetIngredientUnitDisplayName(ingredient.Unit));
            }

        return KitchenProductionPlanResult.Ok(ingredient.Id);
    }

    private async Task<IReadOnlyList<KitchenProductionDemandRow>> BuildDemandRowsAsync(
        DateOnly planDate)
    {
        var subscriptionRows = await dbContext.KitchenMealPlanItems
            .AsNoTracking()
            .Where(item =>
                item.KitchenMealPlanDay.PlanDate == planDate &&
                !item.IsSkipped &&
                item.KitchenMealPlanDay.KitchenMealPlan.KitchenSubscription.Status !=
                    KitchenSubscriptionStatus.Cancelled)
            .GroupBy(item => new
            {
                item.KitchenMenuItemId,
                item.ProductNameSnapshot
            })
            .Select(group => new
            {
                group.Key.KitchenMenuItemId,
                ProductName = group.Key.ProductNameSnapshot,
                Quantity = group.Sum(item => item.Quantity)
            })
            .ToListAsync();

        var orderRows = await dbContext.OrderItems
            .AsNoTracking()
            .Where(item =>
                item.ItemType == CartItemType.KitchenMenuItem &&
                item.KitchenMenuItemId != null &&
                item.Order.DeliveryDate == planDate &&
                item.Order.PaymentStatus == PaymentStatus.Paid &&
                (item.Order.Status == OrderStatus.Confirmed ||
                 item.Order.Status == OrderStatus.Preparing ||
                 item.Order.Status == OrderStatus.OutForDelivery))
            .GroupBy(item => new
            {
                KitchenMenuItemId = item.KitchenMenuItemId!.Value,
                item.ProductName
            })
            .Select(group => new
            {
                group.Key.KitchenMenuItemId,
                group.Key.ProductName,
                Quantity = group.Sum(item => item.Quantity)
            })
            .ToListAsync();

        return subscriptionRows
            .Select(row => new KitchenProductionDemandRow(
                row.KitchenMenuItemId,
                row.ProductName,
                row.Quantity,
                0))
            .Concat(orderRows.Select(row => new KitchenProductionDemandRow(
                row.KitchenMenuItemId,
                row.ProductName,
                0,
                row.Quantity)))
            .ToList();
    }

    private async Task<IReadOnlyList<KitchenProductionRecipeRow>> BuildRecipeRowsAsync(
        IReadOnlyList<int> kitchenMenuItemIds)
    {
        if (kitchenMenuItemIds.Count == 0)
        {
            return [];
        }

        return await dbContext.KitchenRecipeIngredients
            .AsNoTracking()
            .Where(recipe => kitchenMenuItemIds.Contains(recipe.KitchenMenuItemId))
            .Select(recipe => new KitchenProductionRecipeRow(
                recipe.KitchenMenuItemId,
                recipe.KitchenIngredientId,
                recipe.KitchenIngredient.Name,
                recipe.KitchenIngredient.Unit,
                recipe.QuantityPerPortion,
                recipe.KitchenIngredient.CurrentStockQuantity))
            .ToListAsync();
    }

    private static string FormatQuantity(decimal value)
    {
        return value.ToString("0.###");
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

public record KitchenProductionPlanResult(
    bool Succeeded,
    int? Id,
    string? Message)
{
    public static KitchenProductionPlanResult Ok(int id)
    {
        return new KitchenProductionPlanResult(true, id, null);
    }

    public static KitchenProductionPlanResult Fail(string message)
    {
        return new KitchenProductionPlanResult(false, null, message);
    }
}

public record KitchenProductionDemandRow(
    int KitchenMenuItemId,
    string ProductName,
    int SubscriptionPortions,
    int OrderPortions);

public record KitchenProductionRecipeRow(
    int KitchenMenuItemId,
    int KitchenIngredientId,
    string IngredientName,
    KitchenIngredientUnit Unit,
    decimal QuantityPerPortion,
    decimal CurrentStockQuantity);

public record KitchenProductionPlanDraft(
    IReadOnlyList<KitchenProductionPlanItemDraft> Items,
    IReadOnlyList<KitchenProductionPlanMaterialDraft> Materials);

public record KitchenProductionPlanItemDraft(
    int KitchenMenuItemId,
    string ProductName,
    int SubscriptionPortions,
    int OrderPortions,
    int TotalPortions,
    bool HasRecipe);

public record KitchenProductionPlanMaterialDraft(
    int KitchenIngredientId,
    string IngredientName,
    KitchenIngredientUnit Unit,
    decimal RequiredQuantity,
    decimal StockQuantity,
    decimal MissingQuantity);

public static class KitchenProductionPlanCalculator
{
    public static KitchenProductionPlanDraft Calculate(
        IReadOnlyList<KitchenProductionDemandRow> demandRows,
        IReadOnlyList<KitchenProductionRecipeRow> recipeRows)
    {
        var recipeMenuItemIds = recipeRows
            .Select(recipe => recipe.KitchenMenuItemId)
            .ToHashSet();

        var items = demandRows
            .GroupBy(row => row.KitchenMenuItemId)
            .Select(group =>
            {
                var firstRow = group.First();
                var subscriptionPortions = group.Sum(row => row.SubscriptionPortions);
                var orderPortions = group.Sum(row => row.OrderPortions);

                return new KitchenProductionPlanItemDraft(
                    firstRow.KitchenMenuItemId,
                    firstRow.ProductName,
                    subscriptionPortions,
                    orderPortions,
                    subscriptionPortions + orderPortions,
                    recipeMenuItemIds.Contains(firstRow.KitchenMenuItemId));
            })
            .Where(item => item.TotalPortions > 0)
            .OrderBy(item => item.ProductName)
            .ToList();

        var totalPortionsByMenuItemId = items
            .ToDictionary(item => item.KitchenMenuItemId, item => item.TotalPortions);

        var materials = recipeRows
            .Where(recipe => totalPortionsByMenuItemId.ContainsKey(recipe.KitchenMenuItemId))
            .GroupBy(recipe => recipe.KitchenIngredientId)
            .Select(group =>
            {
                var firstRow = group.First();
                var requiredQuantity = group.Sum(recipe =>
                    recipe.QuantityPerPortion * totalPortionsByMenuItemId[recipe.KitchenMenuItemId]);

                return new KitchenProductionPlanMaterialDraft(
                    firstRow.KitchenIngredientId,
                    firstRow.IngredientName,
                    firstRow.Unit,
                    requiredQuantity,
                    firstRow.CurrentStockQuantity,
                    CalculateProductionMissingQuantity(requiredQuantity, firstRow.CurrentStockQuantity));
            })
            .OrderBy(material => material.IngredientName)
            .ToList();

        return new KitchenProductionPlanDraft(items, materials);
    }

    public static decimal CalculateProductionMissingQuantity(
        decimal requiredQuantity,
        decimal currentStockQuantity)
    {
        return Math.Max(0, requiredQuantity - currentStockQuantity);
    }

    public static decimal CalculateMinimumStockDeficit(
        decimal minimumStockQuantity,
        decimal currentStockQuantity)
    {
        return Math.Max(0, minimumStockQuantity - currentStockQuantity);
    }

    public static decimal CalculateSuggestedStockEntryQuantity(
        decimal requiredQuantity,
        decimal currentStockQuantity,
        decimal minimumStockQuantity)
    {
        var productionMissingQuantity = CalculateProductionMissingQuantity(
            requiredQuantity,
            currentStockQuantity);
        var minimumStockDeficit = CalculateMinimumStockDeficit(
            minimumStockQuantity,
            currentStockQuantity);

        return Math.Max(productionMissingQuantity, minimumStockDeficit);
    }
}
