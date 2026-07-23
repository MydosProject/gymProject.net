using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NO23.Web.Data;
using NO23.Web.Domain.Enums;
using NO23.Web.Services;
using NO23.Web.ViewModels.GuestOrders;

namespace NO23.Web.Controllers;

[AllowAnonymous]
public class ShopController(
    ApplicationDbContext dbContext,
    CommerceService commerceService) : Controller
{
    public async Task<IActionResult> Index()
    {
        var products = await dbContext.ShopProducts
            .AsNoTracking()
            .Where(product => product.IsActive)
            .OrderBy(product => product.DisplayOrder)
            .ThenBy(product => product.Name)
            .Select(product => new GuestOrderPageViewModel
            {
                ItemId = product.Id,
                ItemName = product.Name,
                Description = product.Description,
                Category = product.Category,
                UnitPrice = product.UnitPrice,
                ImageUrl = product.ImageUrl,
                StockQuantity = product.StockQuantity
            })
            .ToListAsync();

        return View(products);
    }

    public async Task<IActionResult> Order(int productId)
    {
        var model = await BuildShopOrderPageAsync(productId);

        return model is null ? NotFound() : View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PlaceOrder(
        int productId,
        [Bind(Prefix = "input")] GuestOrderInputViewModel input)
    {
        var model = await BuildShopOrderPageAsync(productId, input);

        if (model is null)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            return View("Order", model);
        }

        var result = await commerceService.CreateGuestShopOrderAsync(
            productId,
            input.Quantity,
            input);

        if (!result.Succeeded || result.EntityId is null)
        {
            ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "Sipariş oluşturulamadı.");
            return View("Order", model);
        }

        var orderNumber = await dbContext.Orders
            .AsNoTracking()
            .Where(order => order.Id == result.EntityId)
            .Select(order => order.OrderNumber)
            .FirstAsync();

        return RedirectToAction(nameof(Confirmation), new { orderNumber });
    }

    public async Task<IActionResult> Confirmation(string orderNumber)
    {
        if (string.IsNullOrWhiteSpace(orderNumber))
        {
            return NotFound();
        }

        var order = await dbContext.Orders
            .AsNoTracking()
            .Include(item => item.Items)
            .FirstOrDefaultAsync(order =>
                order.OrderNumber == orderNumber &&
                order.GuestEmail != null);

        if (order is null)
        {
            return NotFound();
        }

        var orderItem = order.Items
            .OrderBy(item => item.Id)
            .FirstOrDefault();

        if (orderItem is null)
        {
            return NotFound();
        }

        return View(new GuestOrderConfirmationViewModel
        {
            OrderNumber = order.OrderNumber,
            ProductName = orderItem.ProductName,
            Quantity = orderItem.Quantity,
            Total = order.Total,
            DeliveryDate = order.DeliveryDate,
            DeliveryTimeSlot = order.DeliveryTimeSlot,
            PaymentStatusText = GetPaymentStatusLabel(order.PaymentStatus)
        });
    }

    private async Task<GuestOrderPageViewModel?> BuildShopOrderPageAsync(
        int productId,
        GuestOrderInputViewModel? input = null)
    {
        var product = await dbContext.ShopProducts
            .AsNoTracking()
            .Where(product => product.Id == productId && product.IsActive)
            .Select(product => new
            {
                ItemId = product.Id,
                ItemName = product.Name,
                Description = product.Description,
                Category = product.Category,
                UnitPrice = product.UnitPrice,
                ImageUrl = product.ImageUrl,
                StockQuantity = product.StockQuantity
            })
            .FirstOrDefaultAsync();

        return product is null
            ? null
            : new GuestOrderPageViewModel
            {
                ItemId = product.ItemId,
                ItemName = product.ItemName,
                Description = product.Description,
                Category = product.Category,
                UnitPrice = product.UnitPrice,
                ImageUrl = product.ImageUrl,
                StockQuantity = product.StockQuantity,
                Input = input ?? new GuestOrderInputViewModel()
            };
    }

    private static string GetPaymentStatusLabel(PaymentStatus status)
    {
        return status switch
        {
            PaymentStatus.Pending => "Ödeme Bekliyor",
            PaymentStatus.Paid => "Ödendi",
            PaymentStatus.Failed => "Ödeme Başarısız",
            PaymentStatus.Refunded => "İade Edildi",
            _ => status.ToString()
        };
    }
}
