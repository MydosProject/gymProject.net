using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NO23.Web.Data;
using NO23.Web.Domain.Enums;
using NO23.Web.Services;
using NO23.Web.ViewModels.GuestOrders;
using NO23.Web.Services.Payments;

namespace NO23.Web.Controllers;

[AllowAnonymous]
public class KitchenController(
    ApplicationDbContext dbContext,
    CommerceService commerceService,
    IyzicoPaymentService iyzicoPaymentService) : Controller
{
    public async Task<IActionResult> Index()
    {
        var menuItems = await dbContext.KitchenMenuItems
            .AsNoTracking()
            .Where(item => item.IsActive)
            .OrderBy(item => item.DisplayOrder)
            .ThenBy(item => item.Name)
            .Select(item => new
            {
                ItemId = item.Id,
                ItemName = item.Name,
                Description = item.Description,
                item.Category,
                UnitPrice = item.UnitPrice,
                Calories = item.Calories,
                ProteinGrams = item.ProteinGrams,
                CarbohydrateGrams = item.CarbohydrateGrams,
                FatGrams = item.FatGrams,
                Ingredients = item.Ingredients,
                AllergenNames = item.MenuItemAllergens.OrderBy(x => x.KitchenAllergen.DisplayOrder)
                    .Select(x => x.KitchenAllergen.Name).ToList()
            })
            .ToListAsync();

        var model = menuItems
            .Select(item => new GuestOrderPageViewModel
            {
                ItemId = item.ItemId,
                ItemName = item.ItemName,
                Description = item.Description,
                Category = GetMenuCategoryLabel(item.Category),
                UnitPrice = item.UnitPrice,
                Calories = item.Calories,
                ProteinGrams = item.ProteinGrams,
                CarbohydrateGrams = item.CarbohydrateGrams,
                FatGrams = item.FatGrams,
                Ingredients = item.Ingredients,
                Allergens = string.Join(", ", item.AllergenNames)
            })
            .ToList();

        return View(model);
    }

    public async Task<IActionResult> Order(int menuItemId)
    {
        var model = await BuildKitchenOrderPageAsync(menuItemId);

        return model is null ? NotFound() : View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PlaceOrder(
        int menuItemId,
        [Bind(Prefix = "input")] GuestOrderInputViewModel input)
    {
    var model = await BuildKitchenOrderPageAsync(menuItemId, input);

    if (model is null)
    {
        return NotFound();
    }

    if (!ModelState.IsValid)
    {
        return View("Order", model);
    }

    var result = await commerceService.CreateGuestKitchenOrderAsync(
        menuItemId,
        input.Quantity,
        input);

    if (!result.Succeeded || result.EntityId is null)
    {
        ModelState.AddModelError(
            string.Empty,
            result.ErrorMessage ?? "Sipariş oluşturulamadı.");

        return View("Order", model);
    }

    var orderNumber = await dbContext.Orders
        .AsNoTracking()
        .Where(order => order.Id == result.EntityId)
        .Select(order => order.OrderNumber)
        .FirstAsync();

    var returnUrl = Url.Action(
        nameof(KitchenController.Confirmation),
        "Kitchen",
        new
        {
            orderNumber
        },
        Request.Scheme,
        Request.Host.Value);

    var paymentResult =
        await iyzicoPaymentService.InitializeAsync(
            result.EntityId.Value,
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            returnUrl);

    if (!paymentResult.Succeeded ||
        string.IsNullOrWhiteSpace(paymentResult.RedirectUrl))
    {
        ModelState.AddModelError(
            string.Empty,
            paymentResult.ErrorMessage
            ?? "Ödeme başlatılamadı. Lütfen tekrar dene.");

        return View("Order", model);
    }

    return Redirect(paymentResult.RedirectUrl);
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

        if (order.DeliveryDate is null ||string.IsNullOrWhiteSpace(order.DeliveryTimeSlot))
        {
            return NotFound();
        }

        return View(new GuestOrderConfirmationViewModel
        {
            OrderNumber = order.OrderNumber,
            ProductName = orderItem.ProductName,
            Quantity = orderItem.Quantity,
            Total = order.Total,
            DeliveryDate = order.DeliveryDate.Value,
            DeliveryTimeSlot = order.DeliveryTimeSlot,
            PaymentStatus = order.PaymentStatus,
            PaymentStatusText = GetPaymentStatusLabel(order.PaymentStatus)
        });
    }

    private async Task<GuestOrderPageViewModel?> BuildKitchenOrderPageAsync(
        int menuItemId,
        GuestOrderInputViewModel? input = null)
    {
        var menuItem = await dbContext.KitchenMenuItems
            .AsNoTracking()
            .Where(item => item.Id == menuItemId && item.IsActive)
            .Select(item => new
            {
                ItemId = item.Id,
                ItemName = item.Name,
                Description = item.Description,
                item.Category,
                UnitPrice = item.UnitPrice,
                Calories = item.Calories,
                ProteinGrams = item.ProteinGrams,
                CarbohydrateGrams = item.CarbohydrateGrams,
                FatGrams = item.FatGrams,
                Ingredients = item.Ingredients,
                AllergenNames = item.MenuItemAllergens.OrderBy(x => x.KitchenAllergen.DisplayOrder)
                    .Select(x => x.KitchenAllergen.Name).ToList()
            })
            .FirstOrDefaultAsync();

        return menuItem is null
            ? null
            : new GuestOrderPageViewModel
            {
                ItemId = menuItem.ItemId,
                ItemName = menuItem.ItemName,
                Description = menuItem.Description,
                Category = GetMenuCategoryLabel(menuItem.Category),
                UnitPrice = menuItem.UnitPrice,
                Calories = menuItem.Calories,
                ProteinGrams = menuItem.ProteinGrams,
                CarbohydrateGrams = menuItem.CarbohydrateGrams,
                FatGrams = menuItem.FatGrams,
                Ingredients = menuItem.Ingredients,
                Allergens = string.Join(", ", menuItem.AllergenNames),
                Input = input ?? new GuestOrderInputViewModel()
            };
    }

    private static string GetMenuCategoryLabel(MenuItemCategory category)
    {
        return category switch
        {
            MenuItemCategory.Breakfast => "Kahvaltı",
            MenuItemCategory.MainMeal => "Ana Öğün",
            MenuItemCategory.Snack => "Ara Öğün",
            MenuItemCategory.Dessert => "Tatlı",
            MenuItemCategory.Beverage => "İçecek",
            _ => category.ToString()
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
        PaymentStatus.Expired => "Ödeme Süresi Doldu",
        _ => status.ToString()
        };
    }
}
