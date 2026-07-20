using Microsoft.EntityFrameworkCore;
using NO23.Web.Data;
using NO23.Web.Domain.Entities;
using NO23.Web.Domain.Enums;

namespace NO23.Web.Services;

public class CommerceService(ApplicationDbContext dbContext)
{
    private const decimal DeliveryFee = 0;

    public async Task<CommerceResult> AddShopProductToCartAsync(string userId, int productId, int quantity)
    {
        if (quantity <= 0)
        {
            return CommerceResult.Fail("Quantity must be greater than zero.");
        }

        var profile = await GetMemberProfileAsync(userId);

        if (profile is null)
        {
            return CommerceResult.Fail("Member profile was not found.");
        }

        var product = await dbContext.ShopProducts
            .FirstOrDefaultAsync(item => item.Id == productId && item.IsActive);

        if (product is null)
        {
            return CommerceResult.Fail("Product was not found.");
        }

        if (product.StockQuantity < quantity)
        {
            return CommerceResult.Fail("Insufficient product stock.");
        }

        var cart = await GetOrCreateCartAsync(profile.Id);
        var existingItem = cart.Items.FirstOrDefault(item =>
            item.ItemType == CartItemType.ShopProduct &&
            item.ShopProductId == product.Id);

        if (existingItem is null)
        {
            cart.Items.Add(new CartItem
            {
                ItemType = CartItemType.ShopProduct,
                ShopProductId = product.Id,
                ProductName = product.Name,
                UnitPrice = product.UnitPrice,
                Quantity = quantity
            });
        }
        else
        {
            if (product.StockQuantity < existingItem.Quantity + quantity)
            {
                return CommerceResult.Fail("Insufficient product stock.");
            }

            existingItem.Quantity += quantity;
            existingItem.UnitPrice = product.UnitPrice;
            existingItem.ProductName = product.Name;
            existingItem.UpdatedAtUtc = DateTime.UtcNow;
        }

        cart.UpdatedAtUtc = DateTime.UtcNow;
        await dbContext.SaveChangesAsync();

        return CommerceResult.Ok(cart.Id);
    }

    public async Task<CommerceResult> AddKitchenMenuItemToCartAsync(string userId, int menuItemId, int quantity)
    {
        if (quantity <= 0)
        {
            return CommerceResult.Fail("Quantity must be greater than zero.");
        }

        var profile = await GetMemberProfileAsync(userId);

        if (profile is null)
        {
            return CommerceResult.Fail("Member profile was not found.");
        }

        var menuItem = await dbContext.KitchenMenuItems
            .FirstOrDefaultAsync(item => item.Id == menuItemId && item.IsActive);

        if (menuItem is null)
        {
            return CommerceResult.Fail("Kitchen menu item was not found.");
        }

        var cart = await GetOrCreateCartAsync(profile.Id);
        var existingItem = cart.Items.FirstOrDefault(item =>
            item.ItemType == CartItemType.KitchenMenuItem &&
            item.KitchenMenuItemId == menuItem.Id);

        if (existingItem is null)
        {
            cart.Items.Add(new CartItem
            {
                ItemType = CartItemType.KitchenMenuItem,
                KitchenMenuItemId = menuItem.Id,
                ProductName = menuItem.Name,
                UnitPrice = menuItem.UnitPrice,
                Quantity = quantity
            });
        }
        else
        {
            existingItem.Quantity += quantity;
            existingItem.ProductName = menuItem.Name;
            existingItem.UnitPrice = menuItem.UnitPrice;
            existingItem.UpdatedAtUtc = DateTime.UtcNow;
        }

        cart.UpdatedAtUtc = DateTime.UtcNow;
        await dbContext.SaveChangesAsync();

        return CommerceResult.Ok(cart.Id);
    }

    public async Task<CommerceResult> RemoveCartItemAsync(string userId, int cartItemId)
    {
        var profile = await GetMemberProfileAsync(userId);

        if (profile is null)
        {
            return CommerceResult.Fail("Member profile was not found.");
        }

        var cartItem = await dbContext.CartItems
            .Include(item => item.ShoppingCart)
            .FirstOrDefaultAsync(item =>
                item.Id == cartItemId &&
                item.ShoppingCart.MemberProfileId == profile.Id);

        if (cartItem is null)
        {
            return CommerceResult.Fail("Cart item was not found.");
        }

        dbContext.CartItems.Remove(cartItem);
        cartItem.ShoppingCart.UpdatedAtUtc = DateTime.UtcNow;
        await dbContext.SaveChangesAsync();

        return CommerceResult.Ok(cartItem.ShoppingCartId);
    }

    public async Task<CommerceResult> CreateOneTimeOrderFromCartAsync(string userId, DeliveryDetails deliveryDetails)
    {
        var profile = await GetMemberProfileAsync(userId);

        if (profile is null)
        {
            return CommerceResult.Fail("Member profile was not found.");
        }

        var cart = await dbContext.ShoppingCarts
            .Include(item => item.Items)
            .ThenInclude(item => item.ShopProduct)
            .Include(item => item.Items)
            .ThenInclude(item => item.KitchenMenuItem)
            .FirstOrDefaultAsync(item => item.MemberProfileId == profile.Id);

        if (cart is null || cart.Items.Count == 0)
        {
            return CommerceResult.Fail("Cart is empty.");
        }

        foreach (var cartItem in cart.Items.Where(item => item.ItemType == CartItemType.ShopProduct))
        {
            if (cartItem.ShopProduct is null || cartItem.ShopProduct.StockQuantity < cartItem.Quantity)
            {
                return CommerceResult.Fail($"{cartItem.ProductName} does not have enough stock.");
            }
        }

        var order = BuildOrder(profile.Id, OrderType.OneTime, deliveryDetails, null, cart.Items);

        foreach (var cartItem in cart.Items.Where(item => item.ItemType == CartItemType.ShopProduct))
        {
            cartItem.ShopProduct!.StockQuantity -= cartItem.Quantity;
            cartItem.ShopProduct.UpdatedAtUtc = DateTime.UtcNow;
        }

        dbContext.Orders.Add(order);
        dbContext.CartItems.RemoveRange(cart.Items);
        dbContext.ShoppingCarts.Remove(cart);
        await dbContext.SaveChangesAsync();

        return CommerceResult.Ok(order.Id);
    }

    public async Task<CommerceResult> CreateKitchenSubscriptionOrderAsync(
        string userId,
        int kitchenSubscriptionId,
        DeliveryDetails deliveryDetails)
    {
        var profile = await GetMemberProfileAsync(userId);

        if (profile is null)
        {
            return CommerceResult.Fail("Member profile was not found.");
        }

        var subscription = await dbContext.KitchenSubscriptions
            .FirstOrDefaultAsync(item =>
                item.Id == kitchenSubscriptionId &&
                item.MemberProfileId == profile.Id &&
                item.Status == KitchenSubscriptionStatus.Active);

        if (subscription is null)
        {
            return CommerceResult.Fail("Active kitchen subscription was not found.");
        }

        var order = BuildOrder(profile.Id, OrderType.KitchenSubscription, deliveryDetails, subscription.Id, []);

        dbContext.Orders.Add(order);
        await dbContext.SaveChangesAsync();

        return CommerceResult.Ok(order.Id);
    }

    private async Task<MemberProfile?> GetMemberProfileAsync(string userId)
    {
        return await dbContext.MemberProfiles
            .FirstOrDefaultAsync(member => member.ApplicationUserId == userId);
    }

    private async Task<ShoppingCart> GetOrCreateCartAsync(int memberProfileId)
    {
        var cart = await dbContext.ShoppingCarts
            .Include(item => item.Items)
            .FirstOrDefaultAsync(item => item.MemberProfileId == memberProfileId);

        if (cart is not null)
        {
            return cart;
        }

        cart = new ShoppingCart
        {
            MemberProfileId = memberProfileId
        };

        dbContext.ShoppingCarts.Add(cart);
        return cart;
    }

    private static Order BuildOrder(
        int memberProfileId,
        OrderType orderType,
        DeliveryDetails deliveryDetails,
        int? kitchenSubscriptionId,
        IEnumerable<CartItem> cartItems)
    {
        var items = cartItems.Select(item => new OrderItem
        {
            ItemType = item.ItemType,
            KitchenMenuItemId = item.KitchenMenuItemId,
            ShopProductId = item.ShopProductId,
            ProductName = item.ProductName,
            UnitPrice = item.UnitPrice,
            Quantity = item.Quantity,
            LineTotal = item.LineTotal
        }).ToList();

        var subtotal = items.Sum(item => item.LineTotal);

        return new Order
        {
            OrderNumber = GenerateOrderNumber(),
            MemberProfileId = memberProfileId,
            Type = orderType,
            KitchenSubscriptionId = kitchenSubscriptionId,
            DeliveryFullName = deliveryDetails.FullName.Trim(),
            DeliveryPhoneNumber = deliveryDetails.PhoneNumber.Trim(),
            DeliveryAddressLine = deliveryDetails.AddressLine.Trim(),
            DeliveryDistrict = deliveryDetails.District.Trim(),
            DeliveryCity = deliveryDetails.City.Trim(),
            DeliveryPostalCode = deliveryDetails.PostalCode?.Trim(),
            DeliveryDate = deliveryDetails.DeliveryDate,
            DeliveryTimeSlot = deliveryDetails.DeliveryTimeSlot.Trim(),
            Notes = deliveryDetails.Notes?.Trim(),
            Subtotal = subtotal,
            DeliveryFee = DeliveryFee,
            Total = subtotal + DeliveryFee,
            Items = items
        };
    }

    private static string GenerateOrderNumber()
    {
        return $"NO23-{DateTime.UtcNow:yyyyMMddHHmmss}-{Random.Shared.Next(1000, 9999)}";
    }
}
