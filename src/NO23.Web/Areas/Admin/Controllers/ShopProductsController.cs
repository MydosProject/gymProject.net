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
public class ShopProductsController(ApplicationDbContext dbContext) : Controller
{
    public async Task<IActionResult> Index()
    {
        var products = await dbContext.ShopProducts
            .AsNoTracking()
            .OrderBy(product => product.DisplayOrder)
            .ThenBy(product => product.Name)
            .Select(product => new ShopProductListItemViewModel
            {
                Id = product.Id,
                Name = product.Name,
                Sku = product.Sku,
                Category = product.Category,
                UnitPrice = product.UnitPrice,
                StockQuantity = product.StockQuantity,
                MinimumStockQuantity = product.MinimumStockQuantity,
                IsActive = product.IsActive,
                DisplayOrder = product.DisplayOrder,
                VariantSummaries = product.Variants
                    .Where(variant => variant.IsActive)
                    .OrderBy(variant => variant.DisplayOrder)
                    .ThenBy(variant => variant.Size)
                    .Select(variant =>
                        variant.Size + " (" + variant.StockQuantity + ")")
                    .ToList()
            })
            .ToListAsync();

        return View(products);
    }

    public IActionResult Create()
    {
        return View(new ShopProductFormViewModel
        {
            UnitPrice = 500,
            StockQuantity = 10,
            MinimumStockQuantity = 5,
            DisplayOrder = 10,
            IsActive = true
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ShopProductFormViewModel model)
    {
        ValidateVariants(model);

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        if (await dbContext.ShopProducts.AnyAsync(product => product.Sku == model.Sku.Trim()))
        {
            ModelState.AddModelError(nameof(model.Sku), "Bu SKU zaten kullanılıyor.");
            return View(model);
        }

        dbContext.ShopProducts.Add(MapToEntity(model));
        await dbContext.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var product = await dbContext.ShopProducts
            .AsNoTracking()
            .Include(item => item.Variants)
            .FirstOrDefaultAsync(item => item.Id == id);

        if (product is null)
        {
            return NotFound();
        }

        return View(MapToFormModel(product));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, ShopProductFormViewModel model)
    {
        if (id != model.Id)
        {
            return BadRequest();
        }

        ValidateVariants(model);

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        if (await dbContext.ShopProducts.AnyAsync(product =>
            product.Id != id &&
            product.Sku == model.Sku.Trim()))
        {
            ModelState.AddModelError(nameof(model.Sku), "Bu SKU zaten kullanılıyor.");
            return View(model);
        }

        var product = await dbContext.ShopProducts
            .Include(item => item.Variants)
            .FirstOrDefaultAsync(item => item.Id == id);

        if (product is null)
        {
            return NotFound();
        }

        if (model.Variants
            .Where(variant => variant.Id > 0)
            .Any(variant => product.Variants.All(item => item.Id != variant.Id)))
        {
            return BadRequest();
        }

        ApplyFormModel(product, model);
        SynchronizeVariants(product, model.Variants);
        SynchronizeAggregateStock(product, model.StockQuantity);
        await dbContext.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    private static ShopProduct MapToEntity(ShopProductFormViewModel model)
    {
        var product = new ShopProduct
        {
            Variants = model.Variants
                .Select((variant, index) => new ShopProductVariant
                {
                    Size = variant.Size.Trim(),
                    StockQuantity = variant.StockQuantity,
                    IsActive = variant.IsActive,
                    DisplayOrder = index + 1
                })
                .ToList()
        };
        ApplyFormModel(product, model);
        SynchronizeAggregateStock(product, model.StockQuantity);
        return product;
    }

    private static void ApplyFormModel(ShopProduct product, ShopProductFormViewModel model)
    {
        product.Name = model.Name.Trim();
        product.Sku = model.Sku.Trim();
        product.Description = model.Description?.Trim();
        product.Category = model.Category.Trim();
        product.UnitPrice = model.UnitPrice;
        product.MinimumStockQuantity = model.MinimumStockQuantity;
        product.ImageUrl = model.ImageUrl?.Trim();
        product.Tags = model.Tags?.Trim();
        product.IsActive = model.IsActive;
        product.DisplayOrder = model.DisplayOrder;
        product.UpdatedAtUtc = DateTime.UtcNow;
    }

    private static ShopProductFormViewModel MapToFormModel(ShopProduct product)
    {
        return new ShopProductFormViewModel
        {
            Id = product.Id,
            Name = product.Name,
            Sku = product.Sku,
            Description = product.Description,
            Category = product.Category,
            UnitPrice = product.UnitPrice,
            StockQuantity = product.StockQuantity,
            MinimumStockQuantity = product.MinimumStockQuantity,
            ImageUrl = product.ImageUrl,
            Tags = product.Tags,
            IsActive = product.IsActive,
            DisplayOrder = product.DisplayOrder
            ,
            Variants = product.Variants
                .OrderBy(variant => variant.DisplayOrder)
                .ThenBy(variant => variant.Size)
                .Select(variant => new ShopProductVariantFormViewModel
                {
                    Id = variant.Id,
                    Size = variant.Size,
                    StockQuantity = variant.StockQuantity,
                    IsActive = variant.IsActive
                })
                .ToList()
        };
    }

    private void ValidateVariants(ShopProductFormViewModel model)
    {
        var duplicateSizes = model.Variants
            .Where(variant => !string.IsNullOrWhiteSpace(variant.Size))
            .GroupBy(
                variant => variant.Size.Trim(),
                StringComparer.Create(
                    System.Globalization.CultureInfo.GetCultureInfo("tr-TR"),
                    ignoreCase: true))
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToList();

        if (duplicateSizes.Count > 0)
        {
            ModelState.AddModelError(
                nameof(model.Variants),
                $"Aynı beden birden fazla eklenemez: {string.Join(", ", duplicateSizes)}.");
        }
    }

    private static void SynchronizeVariants(
        ShopProduct product,
        IReadOnlyList<ShopProductVariantFormViewModel> submittedVariants)
    {
        var submittedList = submittedVariants.ToList();
        var submittedById = submittedVariants
            .Where(variant => variant.Id > 0)
            .ToDictionary(variant => variant.Id);

        foreach (var existingVariant in product.Variants)
        {
            if (!submittedById.TryGetValue(existingVariant.Id, out var submitted))
            {
                existingVariant.IsActive = false;
                existingVariant.StockQuantity = 0;
                existingVariant.UpdatedAtUtc = DateTime.UtcNow;
                continue;
            }

            existingVariant.Size = submitted.Size.Trim();
            existingVariant.StockQuantity = submitted.StockQuantity;
            existingVariant.IsActive = submitted.IsActive;
            existingVariant.DisplayOrder = submittedList.IndexOf(submitted) + 1;
            existingVariant.UpdatedAtUtc = DateTime.UtcNow;
        }

        foreach (var submitted in submittedVariants.Where(variant => variant.Id == 0))
        {
            product.Variants.Add(new ShopProductVariant
            {
                Size = submitted.Size.Trim(),
                StockQuantity = submitted.StockQuantity,
                IsActive = submitted.IsActive,
                DisplayOrder = submittedList.IndexOf(submitted) + 1
            });
        }
    }

    private static void SynchronizeAggregateStock(
        ShopProduct product,
        int stockWithoutVariants)
    {
        product.StockQuantity = product.Variants.Count == 0
            ? stockWithoutVariants
            : product.Variants
                .Where(variant => variant.IsActive)
                .Sum(variant => variant.StockQuantity);
    }
}
