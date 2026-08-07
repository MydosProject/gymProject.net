using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NO23.Web.Data;
using NO23.Web.Data.Seed;
using NO23.Web.Services;
using NO23.Web.ViewModels.Member;
using NO23.Web.Services.Payments;

namespace NO23.Web.Areas.Member.Controllers;

[Area("Member")]
[Authorize(Roles = ApplicationRoles.Member)]
public class ShopController(
    ApplicationDbContext dbContext,
    CommerceService commerceService,
    MemberCartQueryService cartQueryService,
    IyzicoPaymentService iyzicoPaymentService) : Controller
{
    public async Task<IActionResult> Index()
    {
        return View(await BuildDashboardAsync(new CheckoutInputViewModel()));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddShopProduct(int productId, int quantity = 1)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Challenge();
        }

        var result = await commerceService.AddShopProductToCartAsync(userId, productId, quantity);
        return await RespondToCartMutationAsync(
            result,
            userId,
            "Ürün sepetine eklendi.");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddKitchenMenuItem(int menuItemId, int quantity = 1)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Challenge();
        }

        var result = await commerceService.AddKitchenMenuItemToCartAsync(userId, menuItemId, quantity);
        return await RespondToCartMutationAsync(
            result,
            userId,
            "Kitchen öğünü sepetine eklendi.");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveCartItem(int cartItemId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Challenge();
        }

        var result = await commerceService.RemoveCartItemAsync(userId, cartItemId);
        return await RespondToCartMutationAsync(
            result,
            userId,
            "Ürün sepetinden kaldırıldı.");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Checkout([Bind(Prefix = "CheckoutInput")] CheckoutInputViewModel input)
    {
        if (!ModelState.IsValid)
        {
            if (IsAjaxRequest())
            {
                return BadRequest(new
                {
                    succeeded = false,
                    message = "Teslimat bilgilerini kontrol et.",
                    errors = ModelState
                        .Where(item => item.Value?.Errors.Count > 0)
                        .ToDictionary(
                            item => item.Key,
                            item => item.Value!.Errors
                                .Select(error =>
                                    string.IsNullOrWhiteSpace(error.ErrorMessage)
                                        ? "Bu alanı kontrol et."
                                        : error.ErrorMessage)
                                .ToArray())
                });
            }

            return View("Index", await BuildDashboardAsync(input));
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Challenge();
        }
        var result = await commerceService.CreateOneTimeOrderFromCartAsync(
    userId,
    new DeliveryDetails
    {
        FullName = input.FullName,
        PhoneNumber = input.PhoneNumber,
        AddressLine = input.AddressLine,
        District = input.District,
        City = input.City,
        PostalCode = input.PostalCode,
        DeliveryDate = input.DeliveryDate,
        DeliveryTimeSlot = input.DeliveryTimeSlot,
        Notes = input.Notes
    });

    if (!result.Succeeded || result.EntityId is null)
    {
        return await RespondToCartMutationAsync(
            result,
            userId,
            string.Empty);
    }

    var returnUrl = Url.Action(
    "Index",
    "Orders",
    new
    {
        area = "Member"
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
        var failedResult =
            CommerceResult.Fail(
                paymentResult.ErrorMessage
                ?? "Ödeme başlatılamadı. Lütfen tekrar dene.");

        return await RespondToCartMutationAsync(
            failedResult,
            userId,
            string.Empty);
    }

    if (IsAjaxRequest())
    {
        return Json(new
        {
            succeeded = true,
            message = "iyzico ödeme sayfasına yönlendiriliyorsun.",
            itemCount =
                await cartQueryService.GetItemCountAsync(userId),
            redirectUrl = paymentResult.RedirectUrl
        });
    }

    return Redirect(paymentResult.RedirectUrl);
    }

    private async Task<ShopDashboardViewModel> BuildDashboardAsync(CheckoutInputViewModel checkoutInput)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var shopProducts = await dbContext.ShopProducts
            .AsNoTracking()
            .Where(product => product.IsActive)
            .OrderBy(product => product.DisplayOrder)
            .ThenBy(product => product.Name)
            .Select(product => new ShopProductCardViewModel
            {
                Id = product.Id,
                Name = product.Name,
                Category = product.Category,
                UnitPrice = product.UnitPrice,
                StockQuantity = product.StockQuantity,
                Description = product.Description,
                ImageUrl = product.ImageUrl,
                Tags = product.Tags
            })
            .ToListAsync();

        var kitchenMenuItems = await dbContext.KitchenMenuItems
            .AsNoTracking()
            .Where(item => item.IsActive)
            .OrderBy(item => item.DisplayOrder)
            .ThenBy(item => item.Name)
            .Select(item => new KitchenMenuItemCardViewModel
            {
                Id = item.Id,
                Name = item.Name,
                Category = item.Category.ToString(),
                Calories = item.Calories,
                UnitPrice = item.UnitPrice,
                ProteinGrams = item.ProteinGrams,
                CarbohydrateGrams = item.CarbohydrateGrams,
                FatGrams = item.FatGrams,
                Ingredients = item.Ingredients,
                Allergens = item.Allergens,
                Tags = item.Tags
            })
            .ToListAsync();

        var cartItems = new List<CartItemViewModel>();

        if (!string.IsNullOrWhiteSpace(userId))
        {
            cartItems = await dbContext.CartItems
                .AsNoTracking()
                .Where(item => item.ShoppingCart.MemberProfile.ApplicationUserId == userId)
                .OrderBy(item => item.CreatedAtUtc)
                .Select(item => new CartItemViewModel
                {
                    Id = item.Id,
                    ItemType = item.ItemType.ToString(),
                    ProductName = item.ProductName,
                    UnitPrice = item.UnitPrice,
                    Quantity = item.Quantity,
                    LineTotal = item.UnitPrice * item.Quantity
                })
                .ToListAsync();
        }

        return new ShopDashboardViewModel
        {
            ShopProducts = shopProducts,
            KitchenMenuItems = kitchenMenuItems,
            CartItems = cartItems,
            CheckoutInput = checkoutInput
        };
    }

    private static string GetLocalizedErrorMessage(string? message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return "İşlem gerçekleştirilemedi. Lütfen tekrar dene.";
        }

        const string insufficientStockSuffix = " does not have enough stock.";

        if (message.EndsWith(insufficientStockSuffix, StringComparison.Ordinal))
        {
            var productName = message[..^insufficientStockSuffix.Length];
            return $"{productName} için yeterli stok bulunmuyor.";
        }

        return message switch
        {
            "Quantity must be greater than zero." => "Ürün adedi sıfırdan büyük olmalı.",
            "Member profile was not found." => "Üye profili bulunamadı.",
            "Product was not found." => "Ürün bulunamadı.",
            "Insufficient product stock." => "Bu ürün için yeterli stok bulunmuyor.",
            "Kitchen menu item was not found." => "Kitchen öğünü bulunamadı.",
            "Cart item was not found." => "Sepet ürünü bulunamadı.",
            "Cart is empty." => "Sepetin boş.",
            "Active kitchen subscription was not found." => "Aktif Kitchen aboneliği bulunamadı.",
            _ => message
        };
    }

    private async Task<IActionResult> RespondToCartMutationAsync(
        CommerceResult result,
        string userId,
        string successMessage)
    {
        var message = result.Succeeded
            ? successMessage
            : GetLocalizedErrorMessage(result.ErrorMessage);

        if (IsAjaxRequest())
        {
            return Json(new
            {
                succeeded = result.Succeeded,
                message,
                itemCount = await cartQueryService.GetItemCountAsync(userId)
            });
        }

        TempData[result.Succeeded ? "SuccessMessage" : "ErrorMessage"] = message;
        return RedirectToAction(nameof(Index));
    }

    private bool IsAjaxRequest()
    {
        return string.Equals(
            Request.Headers["X-Requested-With"],
            "XMLHttpRequest",
            StringComparison.OrdinalIgnoreCase);
    }
}
