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
                DisplayOrder = product.DisplayOrder
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

        var product = await dbContext.ShopProducts.FindAsync(id);

        if (product is null)
        {
            return NotFound();
        }

        ApplyFormModel(product, model);
        await dbContext.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    private static ShopProduct MapToEntity(ShopProductFormViewModel model)
    {
        var product = new ShopProduct();
        ApplyFormModel(product, model);
        return product;
    }

    private static void ApplyFormModel(ShopProduct product, ShopProductFormViewModel model)
    {
        product.Name = model.Name.Trim();
        product.Sku = model.Sku.Trim();
        product.Description = model.Description?.Trim();
        product.Category = model.Category.Trim();
        product.UnitPrice = model.UnitPrice;
        product.StockQuantity = model.StockQuantity;
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
        };
    }
}
