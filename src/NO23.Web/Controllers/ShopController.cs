using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NO23.Web.Data;
using NO23.Web.Domain.Enums;
using NO23.Web.Services;
using NO23.Web.ViewModels.GuestOrders;
using NO23.Web.Services.Payments;
using Microsoft.Extensions.Options;

namespace NO23.Web.Controllers;

[AllowAnonymous]
public class ShopController(
    ApplicationDbContext dbContext,
    CommerceService commerceService,
    IyzicoPaymentService iyzicoPaymentService,
    IOptions<IyzicoOptions> paymentOptions,
    IOptions<ClubPickupOptions> clubPickupOptions) : Controller
{
    private readonly IyzicoOptions paymentSettings = paymentOptions.Value;
    private readonly ClubPickupOptions clubPickupSettings = clubPickupOptions.Value;

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

        if (!paymentSettings.Enabled)
        {
            ModelState.AddModelError(
                string.Empty,
                "Online ödeme şu anda kullanılamıyor. Lütfen daha sonra tekrar dene.");
        }

        if (model.ShopVariants.Count > 0 &&
            model.ShopVariants.All(variant =>
                variant.Id != input.ShopProductVariantId ||
                variant.StockQuantity <= 0))
        {
            ModelState.AddModelError(
                "input.ShopProductVariantId",
                "Stokta olan bir beden seçmelisin.");
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
            nameof(Confirmation),
            "Shop",
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
            DeliveryMethodText = GetDeliveryMethodLabel(order.DeliveryMethod),
            PaymentStatus = order.PaymentStatus,
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
                StockQuantity = product.StockQuantity,
                ShopVariants = product.Variants
                    .Where(variant => variant.IsActive)
                    .OrderBy(variant => variant.DisplayOrder)
                    .ThenBy(variant => variant.Size)
                    .Select(variant => new GuestShopVariantViewModel
                    {
                        Id = variant.Id,
                        Size = variant.Size,
                        StockQuantity = variant.StockQuantity
                    })
                    .ToList()
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
                ShopVariants = product.ShopVariants,
                IsPaymentAvailable = paymentSettings.Enabled,
                ClubPickupDisplayName = clubPickupSettings.EffectiveDisplayName,
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
        PaymentStatus.Expired => "Ödeme Süresi Doldu",
        _ => status.ToString()
        };
    }

    private static string GetDeliveryMethodLabel(OrderDeliveryMethod method) =>
        method == OrderDeliveryMethod.ClubPickup
            ? "Salondan teslim"
            : "Adrese teslim";
}
