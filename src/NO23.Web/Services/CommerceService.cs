using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NO23.Web.Data;
using NO23.Web.Domain.Entities;
using NO23.Web.Domain.Enums;
using NO23.Web.ViewModels.GuestOrders;

namespace NO23.Web.Services;

public class CommerceService
(ApplicationDbContext dbContext, IOptions<ClubPickupOptions>? clubPickupOptions = null)
{
    private const decimal DeliveryFee = 0;
    private readonly ClubPickupOptions clubPickupSettings =
        clubPickupOptions?.Value ?? new ClubPickupOptions();

    public async Task<CommerceResult> AddShopProductToCartAsync(
        string userId,
        int productId,
        int quantity,
        int? shopProductVariantId = null)
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
            .Include(item => item.Variants)
            .FirstOrDefaultAsync(item => item.Id == productId && item.IsActive);

        if (product is null)
        {
            return CommerceResult.Fail("Ürün bulunamadı.");
        }

        var activeVariants = product.Variants
            .Where(variant => variant.IsActive)
            .ToList();
        ShopProductVariant? selectedVariant = null;

        if (activeVariants.Count > 0)
        {
            selectedVariant = activeVariants.FirstOrDefault(variant =>
                variant.Id == shopProductVariantId);

            if (selectedVariant is null)
            {
                return CommerceResult.Fail("Geçerli bir beden seçmelisin.");
            }
        }
        else if (shopProductVariantId.HasValue)
        {
            return CommerceResult.Fail("Bu ürün için beden seçimi geçerli değil.");
        }

        var availableStock = selectedVariant?.StockQuantity ?? product.StockQuantity;

        if (availableStock < quantity)
        {
            return CommerceResult.Fail("Ürün stoğu yetersiz.");
        }

        var cart = await GetOrCreateCartAsync(profile.Id);
        var existingItem = cart.Items.FirstOrDefault(item =>
            item.ItemType == CartItemType.ShopProduct &&
            item.ShopProductId == product.Id &&
            item.ShopProductVariantId == selectedVariant?.Id);

        var productName = GetShopProductDisplayName(
            product.Name,
            selectedVariant?.Size);

        if (existingItem is null)
        {
            cart.Items.Add(new CartItem
            {
                ItemType = CartItemType.ShopProduct,
                ShopProductId = product.Id,
                ShopProductVariantId = selectedVariant?.Id,
                ShopProductVariant = selectedVariant,
                SelectedSize = selectedVariant?.Size,
                ProductName = productName,
                UnitPrice = product.UnitPrice,
                Quantity = quantity
            });
        }
        else
        {
            if (availableStock < existingItem.Quantity + quantity)
            {
                return CommerceResult.Fail("Ürün stoğu yetersiz.");
            }

            existingItem.Quantity += quantity;
            existingItem.UnitPrice = product.UnitPrice;
            existingItem.SelectedSize = selectedVariant?.Size;
            existingItem.ProductName = productName;
            existingItem.UpdatedAtUtc = DateTime.UtcNow;
        }

        cart.UpdatedAtUtc = DateTime.UtcNow;
        await dbContext.SaveChangesAsync();

        return CommerceResult.Ok(cart.Id);
    }

    public async Task<CommerceResult> AddKitchenMenuItemToCartAsync(
        string userId,
        int menuItemId,
        int quantity,
        IReadOnlyCollection<int>? removedKitchenIngredientIds = null,
        IReadOnlyCollection<int>? addedKitchenIngredientIds = null)
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
            .Include(item => item.MenuItemAllergens)
                .ThenInclude(item => item.KitchenAllergen)
            .Include(item => item.RecipeIngredients)
                .ThenInclude(item => item.KitchenIngredient)
            .FirstOrDefaultAsync(item => item.Id == menuItemId && item.IsActive);

        if (menuItem is null)
        {
            return CommerceResult.Fail("Kitchen menü ürünü bulunamadı.");
        }

        var conflictNames = await GetAllergenConflictsAsync(profile.Id, menuItem.MenuItemAllergens);
        if (conflictNames.Count > 0)
            return CommerceResult.Fail(
                $"Bu öğün profilinde seçili olan şu alerjenleri içeriyor: {string.Join(", ", conflictNames)}.");

        var customization = await ResolveKitchenCustomizationAsync(
            menuItem,
            removedKitchenIngredientIds,
            addedKitchenIngredientIds);

        if (!customization.Succeeded)
        {
            return CommerceResult.Fail(customization.ErrorMessage!);
        }

        var cart = await GetOrCreateCartAsync(profile.Id);
        var existingItem = cart.Items.FirstOrDefault(item =>
            item.ItemType == CartItemType.KitchenMenuItem &&
            item.KitchenMenuItemId == menuItem.Id &&
            item.RemovedIngredientNames == customization.RemovedIngredientNames &&
            item.AddedIngredientNames == customization.AddedIngredientNames);

        if (existingItem is null)
        {
            cart.Items.Add(new CartItem
            {
                ItemType = CartItemType.KitchenMenuItem,
                KitchenMenuItemId = menuItem.Id,
                ProductName = menuItem.Name,
                RemovedIngredientNames = customization.RemovedIngredientNames,
                AddedIngredientNames = customization.AddedIngredientNames,
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
        var deliveryResult = NormalizeDeliveryDetails(deliveryDetails);

        if (!deliveryResult.Succeeded)
        {
            return deliveryResult;
        }

        var profile = await GetMemberProfileAsync(userId);

        if (profile is null)
        {
            return CommerceResult.Fail("Üye profili bulunamadı.");
        }

        var cart = await dbContext.ShoppingCarts
            .Include(item => item.Items)
            .ThenInclude(item => item.ShopProduct)
            .ThenInclude(product => product!.Variants)
            .Include(item => item.Items)
            .ThenInclude(item => item.ShopProductVariant)
            .Include(item => item.Items)
            .ThenInclude(item => item.KitchenMenuItem)
            .FirstOrDefaultAsync(item => item.MemberProfileId == profile.Id);

        if (cart is null || cart.Items.Count == 0)
        {
            return CommerceResult.Fail("Sepet boş.");
        }

        var menuItemIds = cart.Items.Where(x => x.ItemType == CartItemType.KitchenMenuItem)
            .Select(x => x.KitchenMenuItemId).OfType<int>().ToList();
        var conflict = await dbContext.KitchenMenuItemAllergens.AsNoTracking()
            .Where(x => menuItemIds.Contains(x.KitchenMenuItemId) &&
                x.KitchenAllergen.Members.Any(m => m.MemberProfileId == profile.Id))
            .Select(x => new { x.KitchenMenuItem.Name, AllergenName = x.KitchenAllergen.Name })
            .FirstOrDefaultAsync();
        if (conflict is not null)
            return CommerceResult.Fail(
                $"{conflict.Name} profilinde seçili olan {conflict.AllergenName} alerjenini içeriyor. Sepetini güncelle.");

        foreach (var cartItem in cart.Items.Where(item => item.ItemType == CartItemType.ShopProduct))
        {
            if (cartItem.ShopProduct is null)
            {
                return CommerceResult.Fail($"{cartItem.ProductName} için yeterli stok yok.");
            }

            var hasActiveVariants = cartItem.ShopProduct.Variants
                .Any(variant => variant.IsActive);

            if (hasActiveVariants &&
                (cartItem.ShopProductVariant is null ||
                 !cartItem.ShopProductVariant.IsActive))
            {
                return CommerceResult.Fail(
                    $"{cartItem.ShopProduct.Name} için beden seçimini yenilemelisin.");
            }

            var availableStock = cartItem.ShopProductVariant?.StockQuantity
                ?? cartItem.ShopProduct.StockQuantity;

            if (availableStock < cartItem.Quantity)
            {
                return CommerceResult.Fail($"{cartItem.ProductName} için yeterli stok yok.");
            }
        }

        var order = BuildOrder(profile.Id, OrderType.OneTime, deliveryDetails, null, cart.Items);

        foreach (var cartItem in cart.Items.Where(item => item.ItemType == CartItemType.ShopProduct))
        {
            cartItem.ShopProduct!.StockQuantity -= cartItem.Quantity;
            cartItem.ShopProduct.UpdatedAtUtc = DateTime.UtcNow;

            if (cartItem.ShopProductVariant is not null)
            {
                cartItem.ShopProductVariant.StockQuantity -= cartItem.Quantity;
                cartItem.ShopProductVariant.UpdatedAtUtc = DateTime.UtcNow;
            }
        }

        dbContext.Orders.Add(order);
        await dbContext.SaveChangesAsync();

        return CommerceResult.Ok(order.Id);
    }

        public async Task<CommerceResult> CreateKitchenPackageOrderAsync(
        string userId,
        int kitchenSubscriptionId,
        DeliveryDetails addressDetails)
    {
        var profile = await GetMemberProfileAsync(userId);

        if (profile is null)
        {
            return CommerceResult.Fail(
                "Üye profili bulunamadı.");
        }

        var subscription = await dbContext.KitchenSubscriptions
            .FirstOrDefaultAsync(item =>
                item.Id == kitchenSubscriptionId &&
                item.MemberProfileId == profile.Id);

        if (subscription is null)
        {
            return CommerceResult.Fail(
                "Kitchen paketi bulunamadı.");
        }

        if (subscription.Status !=
            KitchenSubscriptionStatus.PendingPayment)
        {
            return CommerceResult.Fail(
                "Bu Kitchen paketi ödeme beklemiyor.");
        }

        if (subscription.PackagePriceSnapshot <= 0)
        {
            return CommerceResult.Fail(
                "Kitchen paket tutarı geçerli değil.");
        }

        var existingOrder = await dbContext.Orders
            .AsNoTracking()
            .FirstOrDefaultAsync(order =>
                order.KitchenSubscriptionId ==
                    subscription.Id &&
                order.Status == OrderStatus.Pending &&
                order.PaymentStatus ==
                    PaymentStatus.Pending);

        if (existingOrder is not null)
        {
            return CommerceResult.Ok(
                existingOrder.Id);
        }

        var orderItem = new OrderItem
        {
            ItemType =
                CartItemType.KitchenSubscriptionPackage,

            ProductName =
                subscription.PackageNameSnapshot,

            UnitPrice =
                subscription.PackagePriceSnapshot,

            Quantity = 1,

            LineTotal =
                subscription.PackagePriceSnapshot
        };

        var order = BuildOrder(
            profile.Id,
            null,
            OrderType.KitchenSubscription,
            addressDetails,
            subscription.Id,
            [orderItem]);

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
            .Include(item => item.Variants)
            .FirstOrDefaultAsync(item => item.Id == productId && item.IsActive);

        if (product is null)
        {
            return CommerceResult.Fail("Ürün bulunamadı.");
        }

        var activeVariants = product.Variants
            .Where(variant => variant.IsActive)
            .ToList();
        ShopProductVariant? selectedVariant = null;

        if (activeVariants.Count > 0)
        {
            selectedVariant = activeVariants.FirstOrDefault(variant =>
                variant.Id == input.ShopProductVariantId);

            if (selectedVariant is null)
            {
                return CommerceResult.Fail("Geçerli bir beden seçmelisin.");
            }
        }
        else if (input.ShopProductVariantId.HasValue)
        {
            return CommerceResult.Fail("Bu ürün için beden seçimi geçerli değil.");
        }

        var availableStock = selectedVariant?.StockQuantity ?? product.StockQuantity;

        if (availableStock < quantity)
        {
            return CommerceResult.Fail("Ürün stoğu yetersiz.");
        }

        var deliveryDetails = BuildDeliveryDetails(input);
        var deliveryResult = NormalizeDeliveryDetails(deliveryDetails);

        if (!deliveryResult.Succeeded)
        {
            return deliveryResult;
        }

        var orderItem = new OrderItem
        {
            ItemType = CartItemType.ShopProduct,
            ShopProductId = product.Id,
            ShopProductVariantId = selectedVariant?.Id,
            ShopProductVariant = selectedVariant,
            SelectedSize = selectedVariant?.Size,
            ProductName = GetShopProductDisplayName(
                product.Name,
                selectedVariant?.Size),
            UnitPrice = product.UnitPrice,
            Quantity = quantity,
            LineTotal = product.UnitPrice * quantity
        };

        var order = BuildGuestOrder(
            input.Email,
            deliveryDetails,
            orderItem);

        product.StockQuantity -= quantity;
        product.UpdatedAtUtc = DateTime.UtcNow;

        if (selectedVariant is not null)
        {
            selectedVariant.StockQuantity -= quantity;
            selectedVariant.UpdatedAtUtc = DateTime.UtcNow;
        }

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
            .Include(item => item.RecipeIngredients)
                .ThenInclude(item => item.KitchenIngredient)
            .FirstOrDefaultAsync(item => item.Id == menuItemId && item.IsActive);

        if (menuItem is null)
        {
            return CommerceResult.Fail("Kitchen menü ürünü bulunamadı.");
        }

        var customization = await ResolveKitchenCustomizationAsync(
            menuItem,
            input.RemovedKitchenIngredientIds,
            input.AddedKitchenIngredientIds);

        if (!customization.Succeeded)
        {
            return CommerceResult.Fail(customization.ErrorMessage!);
        }

        var deliveryDetails = BuildDeliveryDetails(input);
        var deliveryResult = NormalizeDeliveryDetails(deliveryDetails);

        if (!deliveryResult.Succeeded)
        {
            return deliveryResult;
        }

        var orderItem = new OrderItem
        {
            ItemType = CartItemType.KitchenMenuItem,
            KitchenMenuItemId = menuItem.Id,
            ProductName = menuItem.Name,
            RemovedIngredientNames = customization.RemovedIngredientNames,
            AddedIngredientNames = customization.AddedIngredientNames,
            UnitPrice = menuItem.UnitPrice,
            Quantity = quantity,
            LineTotal = menuItem.UnitPrice * quantity
        };

        var order = BuildGuestOrder(
            input.Email,
            deliveryDetails,
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

    private async Task<List<string>> GetAllergenConflictsAsync(
        int memberProfileId,
        IEnumerable<KitchenMenuItemAllergen> menuAllergens)
    {
        var allergenIds = menuAllergens.Select(x => x.KitchenAllergenId).ToList();
        return await dbContext.MemberAllergens.AsNoTracking()
            .Where(x => x.MemberProfileId == memberProfileId && allergenIds.Contains(x.KitchenAllergenId))
            .OrderBy(x => x.KitchenAllergen.DisplayOrder)
            .Select(x => x.KitchenAllergen.Name).ToListAsync();
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
            ShopProductVariantId = item.ShopProductVariantId,
            SelectedSize = item.SelectedSize,
            RemovedIngredientNames = item.RemovedIngredientNames,
            AddedIngredientNames = item.AddedIngredientNames,
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
            DeliveryMethod = deliveryDetails.DeliveryMethod,
            KitchenSubscriptionId = kitchenSubscriptionId,
            DeliveryFullName = deliveryDetails.FullName.Trim(),
            DeliveryPhoneNumber = deliveryDetails.PhoneNumber.Trim(),
            DeliveryAddressLine = deliveryDetails.AddressLine.Trim(),
            DeliveryDistrict = deliveryDetails.District.Trim(),
            DeliveryCity = deliveryDetails.City.Trim(),
            DeliveryPostalCode = deliveryDetails.PostalCode?.Trim(),
            DeliveryDate = deliveryDetails.DeliveryDate,
            DeliveryTimeSlot = deliveryDetails.DeliveryTimeSlot?.Trim(),
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
            DeliveryMethod = input.DeliveryMethod,
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

    private CommerceResult NormalizeDeliveryDetails(
        DeliveryDetails deliveryDetails)
    {
        if (deliveryDetails.DeliveryMethod ==
            OrderDeliveryMethod.ClubPickup)
        {
            deliveryDetails.AddressLine =
                clubPickupSettings.EffectiveAddressLine;
            deliveryDetails.District =
                clubPickupSettings.EffectiveDistrict;
            deliveryDetails.City =
                clubPickupSettings.EffectiveCity;
            deliveryDetails.PostalCode =
                clubPickupSettings.PostalCode?.Trim();

            return CommerceResult.Ok();
        }

        if (deliveryDetails.DeliveryMethod !=
            OrderDeliveryMethod.AddressDelivery)
        {
            return CommerceResult.Fail(
                "Geçerli bir teslimat yöntemi seçmelisin.");
        }

        if (string.IsNullOrWhiteSpace(deliveryDetails.AddressLine) ||
            string.IsNullOrWhiteSpace(deliveryDetails.District) ||
            string.IsNullOrWhiteSpace(deliveryDetails.City))
        {
            return CommerceResult.Fail(
                "Adres teslimatı için adres, şehir ve ilçe bilgileri zorunludur.");
        }

        return CommerceResult.Ok();
    }

    private static string GenerateOrderNumber()
    {
        return $"NO23-{DateTime.UtcNow:yyyyMMddHHmmss}-{Random.Shared.Next(1000, 9999)}";
    }

    private static string GetShopProductDisplayName(
        string productName,
        string? selectedSize)
    {
        return string.IsNullOrWhiteSpace(selectedSize)
            ? productName
            : $"{productName} · {selectedSize}";
    }

    private async Task<KitchenCustomizationResult> ResolveKitchenCustomizationAsync(
        KitchenMenuItem menuItem,
        IReadOnlyCollection<int>? removedKitchenIngredientIds,
        IReadOnlyCollection<int>? addedKitchenIngredientIds)
    {
        var removedIds = (removedKitchenIngredientIds ?? [])
            .Where(id => id > 0)
            .Distinct()
            .ToList();
        var addedIds = (addedKitchenIngredientIds ?? [])
            .Where(id => id > 0)
            .Distinct()
            .ToList();

        if (removedIds.Count > 20 || addedIds.Count > 20)
        {
            return KitchenCustomizationResult.Fail(
                "Bir sipariş satırında en fazla 20 malzeme seçebilirsin.");
        }

        var recipeIngredients = menuItem.RecipeIngredients
            .Where(item => item.KitchenIngredient is not null)
            .ToList();
        var recipeIngredientIds = recipeIngredients
            .Select(item => item.KitchenIngredientId)
            .ToHashSet();

        if (removedIds.Any(id => !recipeIngredientIds.Contains(id)))
        {
            return KitchenCustomizationResult.Fail(
                "Yalnızca öğünün mevcut reçetesindeki malzemeleri çıkarabilirsin.");
        }

        if (addedIds.Any(recipeIngredientIds.Contains))
        {
            return KitchenCustomizationResult.Fail(
                "Öğünde zaten bulunan bir malzeme ekstra olarak eklenemez.");
        }

        var addedIngredients = addedIds.Count == 0
            ? []
            : await dbContext.KitchenIngredients
                .AsNoTracking()
                .Where(item => item.IsActive && addedIds.Contains(item.Id))
                .OrderBy(item => item.Name)
                .ToListAsync();

        if (addedIngredients.Count != addedIds.Count)
        {
            return KitchenCustomizationResult.Fail(
                "Eklemek istediğin malzemelerden biri artık kullanılamıyor.");
        }

        var removedNames = recipeIngredients
            .Where(item => removedIds.Contains(item.KitchenIngredientId))
            .Select(item => item.KitchenIngredient.Name)
            .OrderBy(name => name)
            .ToList();
        var addedNames = addedIngredients
            .Select(item => item.Name)
            .ToList();

        return KitchenCustomizationResult.Ok(
            JoinIngredientNames(removedNames),
            JoinIngredientNames(addedNames));
    }

    private static string? JoinIngredientNames(IReadOnlyCollection<string> names) =>
        names.Count == 0 ? null : string.Join(", ", names);

    private sealed record KitchenCustomizationResult(
        bool Succeeded,
        string? RemovedIngredientNames,
        string? AddedIngredientNames,
        string? ErrorMessage)
    {
        public static KitchenCustomizationResult Ok(
            string? removedIngredientNames,
            string? addedIngredientNames) =>
            new(true, removedIngredientNames, addedIngredientNames, null);

        public static KitchenCustomizationResult Fail(string errorMessage) =>
            new(false, null, null, errorMessage);
    }
}
