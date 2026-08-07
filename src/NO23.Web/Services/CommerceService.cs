using Microsoft.EntityFrameworkCore;
using NO23.Web.Data;
using NO23.Web.Domain.Entities;
using NO23.Web.Domain.Enums;
using NO23.Web.ViewModels.GuestOrders;

namespace NO23.Web.Services;

public class CommerceService(ApplicationDbContext dbContext)
{
    private const decimal DeliveryFee = 0;

    public async Task<CommerceResult> AddShopProductToCartAsync(string userId, int productId, int quantity)
    {
        if (quantity <= 0)
        {
            return CommerceResult.Fail("Adet sıfırdan büyük olmalıdır.");
        }

        var profile = await GetMemberProfileAsync(userId);

        if (profile is null)
        {
            return CommerceResult.Fail("Üye profili bulunamadı.");
        }

        var product = await dbContext.ShopProducts
            .FirstOrDefaultAsync(item => item.Id == productId && item.IsActive);

        if (product is null)
        {
            return CommerceResult.Fail("Ürün bulunamadı.");
        }

        if (product.StockQuantity < quantity)
        {
            return CommerceResult.Fail("Ürün stoğu yetersiz.");
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
                return CommerceResult.Fail("Ürün stoğu yetersiz.");
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
            return CommerceResult.Fail("Adet sıfırdan büyük olmalıdır.");
        }

        var profile = await GetMemberProfileAsync(userId);

        if (profile is null)
        {
            return CommerceResult.Fail("Üye profili bulunamadı.");
        }

        var menuItem = await dbContext.KitchenMenuItems
            .FirstOrDefaultAsync(item => item.Id == menuItemId && item.IsActive);

        if (menuItem is null)
        {
            return CommerceResult.Fail("Kitchen menü ürünü bulunamadı.");
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
            return CommerceResult.Fail("Üye profili bulunamadı.");
        }

        var cartItem = await dbContext.CartItems
            .Include(item => item.ShoppingCart)
            .FirstOrDefaultAsync(item =>
                item.Id == cartItemId &&
                item.ShoppingCart.MemberProfileId == profile.Id);

        if (cartItem is null)
        {
            return CommerceResult.Fail("Sepet ürünü bulunamadı.");
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
            return CommerceResult.Fail("Üye profili bulunamadı.");
        }

        var cart = await dbContext.ShoppingCarts
            .Include(item => item.Items)
            .ThenInclude(item => item.ShopProduct)
            .Include(item => item.Items)
            .ThenInclude(item => item.KitchenMenuItem)
            .FirstOrDefaultAsync(item => item.MemberProfileId == profile.Id);

        if (cart is null || cart.Items.Count == 0)
        {
            return CommerceResult.Fail("Sepet boş.");
        }

        foreach (var cartItem in cart.Items.Where(item => item.ItemType == CartItemType.ShopProduct))
        {
            if (cartItem.ShopProduct is null || cartItem.ShopProduct.StockQuantity < cartItem.Quantity)
            {
                return CommerceResult.Fail($"{cartItem.ProductName} için yeterli stok yok.");
            }
        }

        var order = BuildOrder(profile.Id, OrderType.OneTime, deliveryDetails, null, cart.Items);

        foreach (var cartItem in cart.Items.Where(item => item.ItemType == CartItemType.ShopProduct))
        {
            cartItem.ShopProduct!.StockQuantity -= cartItem.Quantity;
            cartItem.ShopProduct.UpdatedAtUtc = DateTime.UtcNow;
        }

        dbContext.Orders.Add(order);
        await dbContext.SaveChangesAsync();

        return CommerceResult.Ok(order.Id);
    }

    public async Task<CommerceResult> CreateGuestShopOrderAsync(
        int productId,
        int quantity,
        GuestOrderInputViewModel input)
    {
        if (quantity <= 0)
        {
            return CommerceResult.Fail("Adet sıfırdan büyük olmalıdır.");
        }

        var product = await dbContext.ShopProducts
            .FirstOrDefaultAsync(item => item.Id == productId && item.IsActive);

        if (product is null)
        {
            return CommerceResult.Fail("Ürün bulunamadı.");
        }

        if (product.StockQuantity < quantity)
        {
            return CommerceResult.Fail("Ürün stoğu yetersiz.");
        }

        var orderItem = new OrderItem
        {
            ItemType = CartItemType.ShopProduct,
            ShopProductId = product.Id,
            ProductName = product.Name,
            UnitPrice = product.UnitPrice,
            Quantity = quantity,
            LineTotal = product.UnitPrice * quantity
        };

        var order = BuildGuestOrder(
            input.Email,
            BuildDeliveryDetails(input),
            orderItem);

        product.StockQuantity -= quantity;
        product.UpdatedAtUtc = DateTime.UtcNow;

        dbContext.Orders.Add(order);
        await dbContext.SaveChangesAsync();

        return CommerceResult.Ok(order.Id);
    }

    public async Task<CommerceResult> CreateGuestKitchenOrderAsync(
        int menuItemId,
        int quantity,
        GuestOrderInputViewModel input)
    {
        if (quantity <= 0)
        {
            return CommerceResult.Fail("Adet sıfırdan büyük olmalıdır.");
        }

        var menuItem = await dbContext.KitchenMenuItems
            .FirstOrDefaultAsync(item => item.Id == menuItemId && item.IsActive);

        if (menuItem is null)
        {
            return CommerceResult.Fail("Kitchen menü ürünü bulunamadı.");
        }

        var orderItem = new OrderItem
        {
            ItemType = CartItemType.KitchenMenuItem,
            KitchenMenuItemId = menuItem.Id,
            ProductName = menuItem.Name,
            UnitPrice = menuItem.UnitPrice,
            Quantity = quantity,
            LineTotal = menuItem.UnitPrice * quantity
        };

        var order = BuildGuestOrder(
            input.Email,
            BuildDeliveryDetails(input),
            orderItem);

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

        return BuildOrder(memberProfileId, null, orderType, deliveryDetails, kitchenSubscriptionId, items);
    }

    private static Order BuildGuestOrder(
        string guestEmail,
        DeliveryDetails deliveryDetails,
        OrderItem orderItem)
    {
        return BuildOrder(null, guestEmail, OrderType.OneTime, deliveryDetails, null, [orderItem]);
    }

    private static Order BuildOrder(
        int? memberProfileId,
        string? guestEmail,
        OrderType orderType,
        DeliveryDetails deliveryDetails,
        int? kitchenSubscriptionId,
        IEnumerable<OrderItem> orderItems)
    {
        var items = orderItems.ToList();
        var subtotal = items.Sum(item => item.LineTotal);

        return new Order
        {
            OrderNumber = GenerateOrderNumber(),
            MemberProfileId = memberProfileId,
            GuestEmail = guestEmail?.Trim(),
            Type = orderType,
            Status = OrderStatus.Pending,
            PaymentStatus = PaymentStatus.Pending,
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

    private static DeliveryDetails BuildDeliveryDetails(GuestOrderInputViewModel input)
    {
        return new DeliveryDetails
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
        };
    }

    private static string GenerateOrderNumber()
    {
        return $"NO23-{DateTime.UtcNow:yyyyMMddHHmmss}-{Random.Shared.Next(1000, 9999)}";
    }
}
