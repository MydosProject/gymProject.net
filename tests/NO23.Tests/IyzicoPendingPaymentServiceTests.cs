using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NO23.Web.Data;
using NO23.Web.Domain.Entities;
using NO23.Web.Domain.Enums;
using NO23.Web.Services.Payments;
using NO23.Web.Services;

namespace NO23.Tests;

public class IyzicoPendingPaymentServiceTests
{
    [Fact]
    public async Task ProcessExpiredPaymentsAsync_WhenPaymentActuallySucceeded_ConfirmsOrderAndDoesNotRestoreStock()
    {
        await using var dbContext =
            CreateDbContext();

        var product =
            new ShopProduct
            {
                Name = "NO23 Expired Checkout Test",
                Sku = "EXP-SUCCESS-001",
                Category = "Equipment",
                UnitPrice = 100m,

                // Sipariş oluşturulurken:
                // 10 -> 8 olmuş kabul ediyoruz.
                StockQuantity = 8
            };

        var order =
            new Order
            {
                OrderNumber =
                    $"NO23-EXP-{Guid.NewGuid():N}",

                MemberProfileId = null,
                GuestEmail = "expired-success@no23.test",

                Type = OrderType.OneTime,

                Status = OrderStatus.Pending,
                PaymentStatus = PaymentStatus.Pending,

                DeliveryFullName = "Guest Customer",
                DeliveryPhoneNumber = "05551112233",
                DeliveryAddressLine = "Test Sokak No:23",
                DeliveryDistrict = "Kadikoy",
                DeliveryCity = "Istanbul",
                DeliveryPostalCode = "34710",

                DeliveryDate =
                    DateOnly.FromDateTime(
                        DateTime.Today.AddDays(1)),

                DeliveryTimeSlot = "10:00-12:00",

                Subtotal = 200m,
                DeliveryFee = 0m,
                Total = 200m,

                Items =
                [
                    new OrderItem
                    {
                        ItemType =
                            CartItemType.ShopProduct,

                        ShopProduct =
                            product,

                        ProductName =
                            product.Name,

                        UnitPrice =
                            100m,

                        Quantity =
                            2,

                        LineTotal =
                            200m
                    }
                ]
            };

        var payment =
            new PaymentTransaction
            {
                Order = order,
                Provider = "iyzico",
                ConversationId =
                    "expired-success-conversation",
                BasketId =
                    order.OrderNumber,
                Token =
                    "expired-success-token",
                PaymentStatus =
                    PaymentStatus.Pending,
                Amount =
                    order.Total,
                Currency =
                    "TRY",

                // Süresi geçmiş.
                CheckoutExpiresAtUtc =
                    DateTime.UtcNow.AddMinutes(-5)
            };

        dbContext.AddRange(
            product,
            order,
            payment);

        await dbContext.SaveChangesAsync();

        var fakeClient =
            new FakeIyzicoCheckoutClient(
                new IyzicoCheckoutRetrieveResult
                {
                    Succeeded = true,
                    StatusCode = 200,
                    ConversationId =
                        payment.ConversationId,
                    RawStatus = "success",
                    Token =
                        payment.Token,
                    PaymentId =
                        "payment-expired-success",
                    PaymentStatus =
                        "SUCCESS",
                    BasketId =
                        order.OrderNumber,
                    Currency =
                        "TRY",
                    RawResponseJson =
                        "{\"status\":\"success\",\"paymentStatus\":\"SUCCESS\"}"
                });

        var service =
            new IyzicoPendingPaymentService(
                dbContext,
                fakeClient,
                new KitchenPlanMatchingService(dbContext),
                NullLogger<IyzicoPendingPaymentService>.Instance);

        var processed =
            await service.ProcessExpiredPaymentsAsync();

        Assert.Equal(
            1,
            processed);

        var savedOrder =
            await dbContext.Orders
                .SingleAsync();

        Assert.Equal(
            OrderStatus.Confirmed,
            savedOrder.Status);

        Assert.Equal(
            PaymentStatus.Paid,
            savedOrder.PaymentStatus);

        var savedPayment =
            await dbContext.PaymentTransactions
                .SingleAsync();

        Assert.Equal(
            PaymentStatus.Paid,
            savedPayment.PaymentStatus);

        Assert.NotNull(
            savedPayment.CompletedAtUtc);

        Assert.Null(
            savedPayment.ExpiredAtUtc);

        Assert.Null(
            savedPayment.FailedAtUtc);

        // En önemli kontrol:
        // ödeme aslında başarılıysa stok GERİ VERİLMEMELİ.
        var savedProduct =
            await dbContext.ShopProducts
                .SingleAsync();

        Assert.Equal(
            8,
            savedProduct.StockQuantity);

        Assert.Null(
            savedOrder.StockRestoredAtUtc);
    }

    [Fact]
    public async Task ProcessStaleOrphanShopOrdersAsync_WhenOrderIsOlderThanThirtyMinutes_ExpiresOrderAndRestoresStock()
    {
        await using var dbContext =
            CreateDbContext();

        var product =
            new ShopProduct
            {
                Name = "NO23 Orphan Gloves",
                Sku = "ORPHAN-OLD-001",
                Category = "Equipment",
                UnitPrice = 100m,
                StockQuantity = 3
            };

        var order =
            CreatePendingShopOrder(
                product,
                quantity: 2,
                DateTime.UtcNow.AddMinutes(-31));

        dbContext.AddRange(
            product,
            order);

        await dbContext.SaveChangesAsync();

        var service =
            CreateService(
                dbContext);

        var processed =
            await service.ProcessStaleOrphanShopOrdersAsync();

        Assert.Equal(
            1,
            processed);

        var savedOrder =
            await dbContext.Orders
                .SingleAsync();

        Assert.Equal(
            OrderStatus.Cancelled,
            savedOrder.Status);

        Assert.Equal(
            PaymentStatus.Expired,
            savedOrder.PaymentStatus);

        Assert.NotNull(
            savedOrder.StockRestoredAtUtc);

        var savedProduct =
            await dbContext.ShopProducts
                .SingleAsync();

        Assert.Equal(
            5,
            savedProduct.StockQuantity);

        var pendingReservedQuantity =
            await dbContext.OrderItems
                .Where(item =>
                    item.ShopProductId == savedProduct.Id &&
                    item.Order.PaymentStatus == PaymentStatus.Pending &&
                    item.Order.Status == OrderStatus.Pending &&
                    !item.Order.StockRestoredAtUtc.HasValue)
                .Select(item => (int?)item.Quantity)
                .SumAsync()
            ?? 0;

        Assert.Equal(
            0,
            pendingReservedQuantity);
    }

    [Fact]
    public async Task ProcessStaleOrphanShopOrdersAsync_WhenOrderIsNewerThanThirtyMinutes_KeepsOrderPending()
    {
        await using var dbContext =
            CreateDbContext();

        var product =
            new ShopProduct
            {
                Name = "NO23 Recent Orphan Gloves",
                Sku = "ORPHAN-RECENT-001",
                Category = "Equipment",
                UnitPrice = 100m,
                StockQuantity = 3
            };

        var order =
            CreatePendingShopOrder(
                product,
                quantity: 2,
                DateTime.UtcNow.AddMinutes(-29));

        dbContext.AddRange(
            product,
            order);

        await dbContext.SaveChangesAsync();

        var service =
            CreateService(
                dbContext);

        var processed =
            await service.ProcessStaleOrphanShopOrdersAsync();

        Assert.Equal(
            0,
            processed);

        var savedOrder =
            await dbContext.Orders
                .SingleAsync();

        Assert.Equal(
            OrderStatus.Pending,
            savedOrder.Status);

        Assert.Equal(
            PaymentStatus.Pending,
            savedOrder.PaymentStatus);

        Assert.Null(
            savedOrder.StockRestoredAtUtc);

        var savedProduct =
            await dbContext.ShopProducts
                .SingleAsync();

        Assert.Equal(
            3,
            savedProduct.StockQuantity);
    }

    [Fact]
    public async Task ProcessStaleOrphanShopOrdersAsync_WhenRunTwice_RestoresStockOnlyOnce()
    {
        await using var dbContext =
            CreateDbContext();

        var product =
            new ShopProduct
            {
                Name = "NO23 Idempotent Orphan Gloves",
                Sku = "ORPHAN-IDEMPOTENT-001",
                Category = "Equipment",
                UnitPrice = 100m,
                StockQuantity = 3
            };

        var order =
            CreatePendingShopOrder(
                product,
                quantity: 2,
                DateTime.UtcNow.AddMinutes(-31));

        dbContext.AddRange(
            product,
            order);

        await dbContext.SaveChangesAsync();

        var service =
            CreateService(
                dbContext);

        var firstProcessed =
            await service.ProcessStaleOrphanShopOrdersAsync();

        var secondProcessed =
            await service.ProcessStaleOrphanShopOrdersAsync();

        Assert.Equal(
            1,
            firstProcessed);

        Assert.Equal(
            0,
            secondProcessed);

        var savedProduct =
            await dbContext.ShopProducts
                .SingleAsync();

        Assert.Equal(
            5,
            savedProduct.StockQuantity);
    }

    [Fact]
    public async Task ProcessStaleOrphanShopOrdersAsync_WhenOrderHasPaymentTransaction_DoesNotBypassIyzicoReconciliation()
    {
        await using var dbContext =
            CreateDbContext();

        var product =
            new ShopProduct
            {
                Name = "NO23 Payment Pending Gloves",
                Sku = "ORPHAN-WITH-PAYMENT-001",
                Category = "Equipment",
                UnitPrice = 100m,
                StockQuantity = 3
            };

        var order =
            CreatePendingShopOrder(
                product,
                quantity: 2,
                DateTime.UtcNow.AddMinutes(-120));

        var payment =
            new PaymentTransaction
            {
                Order = order,
                Provider = "iyzico",
                ConversationId =
                    "orphan-with-payment-conversation",
                BasketId =
                    order.OrderNumber,
                Token =
                    "orphan-with-payment-token",
                PaymentStatus =
                    PaymentStatus.Pending,
                Amount =
                    order.Total,
                Currency =
                    "TRY",
                CheckoutExpiresAtUtc =
                    DateTime.UtcNow.AddMinutes(-90)
            };

        dbContext.AddRange(
            product,
            order,
            payment);

        await dbContext.SaveChangesAsync();

        var service =
            CreateService(
                dbContext);

        var processed =
            await service.ProcessStaleOrphanShopOrdersAsync();

        Assert.Equal(
            0,
            processed);

        var savedOrder =
            await dbContext.Orders
                .SingleAsync();

        Assert.Equal(
            OrderStatus.Pending,
            savedOrder.Status);

        Assert.Equal(
            PaymentStatus.Pending,
            savedOrder.PaymentStatus);

        Assert.Null(
            savedOrder.StockRestoredAtUtc);

        var savedProduct =
            await dbContext.ShopProducts
                .SingleAsync();

        Assert.Equal(
            3,
            savedProduct.StockQuantity);
    }

    private static ApplicationDbContext CreateDbContext()
    {
        var options =
            new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(
                    $"NO23-IyzicoPending-{Guid.NewGuid()}")
                .Options;

        return new ApplicationDbContext(options);

        
    }

    private static IyzicoPendingPaymentService CreateService(
        ApplicationDbContext dbContext)
    {
        return new IyzicoPendingPaymentService(
            dbContext,
            new FakeIyzicoCheckoutClient(
                new IyzicoCheckoutRetrieveResult
                {
                    Succeeded = true
                }),
            new KitchenPlanMatchingService(dbContext),
            NullLogger<IyzicoPendingPaymentService>.Instance);
    }

    private static Order CreatePendingShopOrder(
        ShopProduct product,
        int quantity,
        DateTime createdAtUtc)
    {
        return new Order
        {
            OrderNumber =
                $"NO23-ORPHAN-{Guid.NewGuid():N}",

            MemberProfileId =
                null,

            GuestEmail =
                "orphan@no23.test",

            Type =
                OrderType.OneTime,

            Status =
                OrderStatus.Pending,

            PaymentStatus =
                PaymentStatus.Pending,

            DeliveryFullName =
                "Orphan Customer",

            DeliveryPhoneNumber =
                "05551112233",

            DeliveryAddressLine =
                "Test Sokak No:23",

            DeliveryDistrict =
                "Kadikoy",

            DeliveryCity =
                "Istanbul",

            DeliveryPostalCode =
                "34710",

            DeliveryDate =
                DateOnly.FromDateTime(
                    DateTime.Today.AddDays(1)),

            DeliveryTimeSlot =
                "10:00-12:00",

            Subtotal =
                product.UnitPrice * quantity,

            DeliveryFee =
                0m,

            Total =
                product.UnitPrice * quantity,

            CreatedAtUtc =
                createdAtUtc,

            Items =
            [
                new OrderItem
                {
                    ItemType =
                        CartItemType.ShopProduct,

                    ShopProduct =
                        product,

                    ProductName =
                        product.Name,

                    UnitPrice =
                        product.UnitPrice,

                    Quantity =
                        quantity,

                    LineTotal =
                        product.UnitPrice * quantity
                }
            ]
        };
    }

    private static async Task<(
    KitchenSubscription Subscription,
    Order Order,
    PaymentTransaction Payment)>
    CreatePendingKitchenPaymentAsync(
        ApplicationDbContext dbContext)
{
    var startsOn =
        DateOnly.FromDateTime(
            DateTime.Today.AddDays(1));

    var subscription =
        new KitchenSubscription
        {
            MemberProfileId = 1,

            KitchenSubscriptionPackageId = 1,

            Plan =
                KitchenSubscriptionPlan.FiveDays,

            Status =
                KitchenSubscriptionStatus.PendingPayment,

            PackageNameSnapshot =
                "5 Günlük Kitchen Paketi",

            PackagePriceSnapshot =
                4250m,

            PackageDaysSnapshot = 5,

            Goal =
                NutritionGoal.WeightMaintenance,

            SourceHeightCm = 170,

            SourceWeightKg = 65m,

            SourceAge = 23,

            SourceGender =
                Gender.Female,

            SourceActivityLevel =
                ActivityLevel.ModeratelyActive,

            DailyCalories = 2000,

            ProteinGrams = 120,

            CarbohydrateGrams = 220,

            FatGrams = 65,

            StartsOn = startsOn,

            EndsOn =
                startsOn.AddDays(4)
        };

    dbContext.KitchenSubscriptions.Add(
        subscription);

    await dbContext.SaveChangesAsync();

    var order =
        new Order
        {
            OrderNumber =
                $"NO23-KITCHEN-PENDING-{Guid.NewGuid():N}",

            Type =
                OrderType.KitchenSubscription,

            Status =
                OrderStatus.Pending,

            PaymentStatus =
                PaymentStatus.Pending,

            KitchenSubscriptionId =
                subscription.Id,

            DeliveryFullName =
                "Kitchen Test User",

            DeliveryPhoneNumber =
                "05551112233",

            DeliveryAddressLine =
                "Test Sokak No:23",

            DeliveryDistrict =
                "Kadikoy",

            DeliveryCity =
                "Istanbul",

            Subtotal = 4250m,

            DeliveryFee = 0m,

            Total = 4250m,

            Items =
            [
                new OrderItem
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
                }
            ]
        };

    var payment =
        new PaymentTransaction
        {
            Order = order,

            Provider =
                "iyzico",

            ConversationId =
                $"kitchen-pending-{Guid.NewGuid():N}",

            BasketId =
                order.OrderNumber,

            Token =
                $"kitchen-token-{Guid.NewGuid():N}",

            PaymentStatus =
                PaymentStatus.Pending,

            Amount =
                order.Total,

            Currency =
                "TRY",

            CheckoutExpiresAtUtc =
                DateTime.UtcNow.AddMinutes(-5)
        };

    dbContext.AddRange(
        order,
        payment);

    await dbContext.SaveChangesAsync();

    return (
        subscription,
        order,
        payment);
}

private static async Task SeedKitchenPlanItemsAsync(
    ApplicationDbContext dbContext)
{
    dbContext.KitchenMenuItems.AddRange(
        new KitchenMenuItem
        {
            Name = "Test Breakfast",
            Category =
                MenuItemCategory.Breakfast,
            Calories = 400,
            ProteinGrams = 25,
            CarbohydrateGrams = 45,
            FatGrams = 12,
            UnitPrice = 200m,
            IsActive = true,
            IsPlanEligible = true,
            DisplayOrder = 1
        },

        new KitchenMenuItem
        {
            Name = "Test Snack A",
            Category =
                MenuItemCategory.Snack,
            Calories = 200,
            ProteinGrams = 15,
            CarbohydrateGrams = 20,
            FatGrams = 7,
            UnitPrice = 150m,
            IsActive = true,
            IsPlanEligible = true,
            DisplayOrder = 2
        },

        new KitchenMenuItem
        {
            Name = "Test Snack B",
            Category =
                MenuItemCategory.Snack,
            Calories = 220,
            ProteinGrams = 17,
            CarbohydrateGrams = 22,
            FatGrams = 8,
            UnitPrice = 160m,
            IsActive = true,
            IsPlanEligible = true,
            DisplayOrder = 3
        },

        new KitchenMenuItem
        {
            Name = "Test Main Meal A",
            Category =
                MenuItemCategory.MainMeal,
            Calories = 550,
            ProteinGrams = 40,
            CarbohydrateGrams = 55,
            FatGrams = 15,
            UnitPrice = 300m,
            IsActive = true,
            IsPlanEligible = true,
            DisplayOrder = 4
        },

        new KitchenMenuItem
        {
            Name = "Test Main Meal B",
            Category =
                MenuItemCategory.MainMeal,
            Calories = 600,
            ProteinGrams = 45,
            CarbohydrateGrams = 60,
            FatGrams = 17,
            UnitPrice = 320m,
            IsActive = true,
            IsPlanEligible = true,
            DisplayOrder = 5
        });

    await dbContext.SaveChangesAsync();
}


    [Fact]
public async Task ProcessExpiredPaymentsAsync_WhenGuestShopCheckoutExpired_CancelsOrderAndRestoresStock()
{
    await using var dbContext =
        CreateDbContext();

    var product =
        new ShopProduct
        {
            Name = "NO23 Oversize Hoodie",
            Sku = "EXP-GUEST-SHOP-001",
            Category = "Apparel",
            UnitPrice = 1850m,

            // Gerçek senaryomuz:
            // başlangıç 10, checkout sırasında 2 rezerve edildi.
            StockQuantity = 8
        };

    var order =
        new Order
        {
            OrderNumber =
                $"NO23-EXP-GUEST-{Guid.NewGuid():N}",

            // Public guest order
            MemberProfileId = null,
            GuestEmail = "expired-guest@no23.test",

            Type = OrderType.OneTime,

            Status = OrderStatus.Pending,
            PaymentStatus = PaymentStatus.Pending,

            DeliveryFullName = "Guest Customer",
            DeliveryPhoneNumber = "05551112233",
            DeliveryAddressLine = "Test Sokak No:23",
            DeliveryDistrict = "Kadikoy",
            DeliveryCity = "Istanbul",
            DeliveryPostalCode = "34710",

            DeliveryDate =
                DateOnly.FromDateTime(
                    DateTime.Today.AddDays(1)),

            DeliveryTimeSlot = "10:00-12:00",

            Subtotal = 3700m,
            DeliveryFee = 0m,
            Total = 3700m,

            Items =
            [
                new OrderItem
                {
                    ItemType =
                        CartItemType.ShopProduct,

                    ShopProduct =
                        product,

                    ProductName =
                        product.Name,

                    UnitPrice =
                        product.UnitPrice,

                    Quantity = 2,

                    LineTotal = 3700m
                }
            ]
        };

    var payment =
        new PaymentTransaction
        {
            Order = order,

            Provider = "iyzico",

            ConversationId =
                "expired-guest-shop-conversation",

            BasketId =
                order.OrderNumber,

            Token =
                "expired-guest-shop-token",

            PaymentStatus =
                PaymentStatus.Pending,

            Amount =
                order.Total,

            Currency =
                "TRY",

            // Checkout süresi geçmiş.
            CheckoutExpiresAtUtc =
                DateTime.UtcNow.AddMinutes(-5)
        };

    dbContext.AddRange(
        product,
        order,
        payment);

    await dbContext.SaveChangesAsync();

    var fakeClient =
        new FakeIyzicoCheckoutClient(
            new IyzicoCheckoutRetrieveResult
            {
                // iyzico API'ye erişebildik.
                Succeeded = true,

                StatusCode = 200,

                ConversationId =
                    payment.ConversationId,

                RawStatus =
                    "success",

                Token =
                    payment.Token,

                BasketId =
                    order.OrderNumber,

                Currency =
                    "TRY",

                // SUCCESS veya FAILURE yok.
                // Checkout tamamlanmamış.
                PaymentStatus =
                    "PENDING",

                RawResponseJson =
                    "{\"status\":\"success\",\"paymentStatus\":\"PENDING\"}"
            });

    var service =
        new IyzicoPendingPaymentService(
            dbContext,
            fakeClient,
            new KitchenPlanMatchingService(dbContext),
            NullLogger<IyzicoPendingPaymentService>.Instance);

    var processed =
        await service.ProcessExpiredPaymentsAsync();

    Assert.Equal(
        1,
        processed);

    var savedOrder =
        await dbContext.Orders
            .SingleAsync();

    // Sipariş artık beklememeli.
    Assert.Equal(
        OrderStatus.Cancelled,
        savedOrder.Status);

    Assert.Equal(
        PaymentStatus.Expired,
        savedOrder.PaymentStatus);

    // Stok yalnızca bir kez geri verilmiş olmalı.
    Assert.NotNull(
        savedOrder.StockRestoredAtUtc);

    var savedPayment =
        await dbContext.PaymentTransactions
            .SingleAsync();

    Assert.Equal(
        PaymentStatus.Expired,
        savedPayment.PaymentStatus);

    Assert.NotNull(
        savedPayment.ExpiredAtUtc);

    Assert.Null(
        savedPayment.FailedAtUtc);

    Assert.Null(
        savedPayment.CompletedAtUtc);

    Assert.Contains(
        "checkout süresi doldu",
        savedPayment.LastError!,
        StringComparison.OrdinalIgnoreCase);

    // EN KRİTİK KONTROL:
    //
    // 10 -> checkout sırasında 8
    // checkout expired -> 10
    var savedProduct =
        await dbContext.ShopProducts
            .SingleAsync();

    Assert.Equal(
        10,
        savedProduct.StockQuantity);
}
[Fact]
public async Task ProcessExpiredPaymentsAsync_WhenGuestKitchenCheckoutExpired_CancelsOrderWithoutShopStockChange()
{
    await using var dbContext =
        CreateDbContext();

    var kitchenItem =
        new KitchenMenuItem
        {
            Name = "Protein Power Bowl",
            Category = MenuItemCategory.MainMeal,
            UnitPrice = 295m,
            Calories = 620,
            ProteinGrams = 42m,
            CarbohydrateGrams = 58m,
            FatGrams = 18m,
            Ingredients = "Chicken, rice, vegetables",
            DisplayOrder = 1,
            IsActive = true
        };

    var order =
        new Order
        {
            OrderNumber =
                $"NO23-EXP-KITCHEN-{Guid.NewGuid():N}",

            MemberProfileId = null,
            GuestEmail = "expired-kitchen@no23.test",

            Type = OrderType.OneTime,

            Status = OrderStatus.Pending,
            PaymentStatus = PaymentStatus.Pending,

            DeliveryFullName = "Guest Customer",
            DeliveryPhoneNumber = "05551112233",
            DeliveryAddressLine = "Test Sokak No:23",
            DeliveryDistrict = "Kadikoy",
            DeliveryCity = "Istanbul",
            DeliveryPostalCode = "34710",

            DeliveryDate =
                DateOnly.FromDateTime(
                    DateTime.Today.AddDays(1)),

            DeliveryTimeSlot = "10:00-12:00",

            Subtotal = 590m,
            DeliveryFee = 0m,
            Total = 590m,

            Items =
            [
                new OrderItem
                {
                    ItemType =
                        CartItemType.KitchenMenuItem,

                    KitchenMenuItem =
                        kitchenItem,

                    ProductName =
                        kitchenItem.Name,

                    UnitPrice =
                        kitchenItem.UnitPrice,

                    Quantity = 2,

                    LineTotal = 590m
                }
            ]
        };

    var payment =
        new PaymentTransaction
        {
            Order = order,

            Provider = "iyzico",

            ConversationId =
                "expired-kitchen-conversation",

            BasketId =
                order.OrderNumber,

            Token =
                "expired-kitchen-token",

            PaymentStatus =
                PaymentStatus.Pending,

            Amount =
                order.Total,

            Currency = "TRY",

            CheckoutExpiresAtUtc =
                DateTime.UtcNow.AddMinutes(-5)
        };

    dbContext.AddRange(
        kitchenItem,
        order,
        payment);

    await dbContext.SaveChangesAsync();

    var fakeClient =
        new FakeIyzicoCheckoutClient(
            new IyzicoCheckoutRetrieveResult
            {
                Succeeded = true,
                StatusCode = 200,

                ConversationId =
                    payment.ConversationId,

                RawStatus = "success",

                Token =
                    payment.Token,

                BasketId =
                    order.OrderNumber,

                Currency = "TRY",

                PaymentStatus =
                    "PENDING",

                RawResponseJson =
                    "{\"status\":\"success\",\"paymentStatus\":\"PENDING\"}"
            });

    var service =
        new IyzicoPendingPaymentService(
            dbContext,
            fakeClient,
            new KitchenPlanMatchingService(dbContext),
            NullLogger<IyzicoPendingPaymentService>.Instance);

    var processed =
        await service.ProcessExpiredPaymentsAsync();

    Assert.Equal(
        1,
        processed);

    var savedOrder =
        await dbContext.Orders
            .SingleAsync();

    Assert.Equal(
        OrderStatus.Cancelled,
        savedOrder.Status);

    Assert.Equal(
        PaymentStatus.Expired,
        savedOrder.PaymentStatus);

    var savedPayment =
        await dbContext.PaymentTransactions
            .SingleAsync();

    Assert.Equal(
        PaymentStatus.Expired,
        savedPayment.PaymentStatus);

    Assert.NotNull(
        savedPayment.ExpiredAtUtc);

    Assert.Null(
        savedPayment.CompletedAtUtc);

    Assert.Null(
        savedPayment.FailedAtUtc);

    // Kitchen siparişinde Shop ürünü yok.
    Assert.False(
        await dbContext.ShopProducts.AnyAsync());
}
[Fact]
public async Task ProcessExpiredPaymentsAsync_WhenMemberMixedCartCheckoutExpired_RestoresOnlyShopProductStock()
{
    await using var dbContext =
        CreateDbContext();

    var shopProduct =
        new ShopProduct
        {
            Name = "NO23 Oversize Hoodie",
            Sku = "EXP-MEMBER-SHOP-001",
            Category = "Apparel",
            UnitPrice = 1850m,

            // Gerçek başlangıç 10.
            // Member checkout'ta 3 adet rezerve edildi:
            // 10 -> 7
            StockQuantity = 7
        };

    var kitchenItem =
        new KitchenMenuItem
        {
            Name = "Protein Power Bowl",
            Category = MenuItemCategory.MainMeal,
            UnitPrice = 295m,
            Calories = 620,
            ProteinGrams = 42m,
            CarbohydrateGrams = 58m,
            FatGrams = 18m,
            Ingredients = "Chicken, rice, vegetables",
            DisplayOrder = 1,
            IsActive = true
        };

    var order =
        new Order
        {
            OrderNumber =
                $"NO23-EXP-MEMBER-{Guid.NewGuid():N}",

            // InMemory testte gerçek MemberProfile
            // oluşturmamız gerekmiyor.
            MemberProfileId = 99,

            GuestEmail = null,

            Type = OrderType.OneTime,

            Status = OrderStatus.Pending,
            PaymentStatus = PaymentStatus.Pending,

            DeliveryFullName = "NO23 Member",
            DeliveryPhoneNumber = "05551112233",
            DeliveryAddressLine = "Member Sokak No:23",
            DeliveryDistrict = "Kadikoy",
            DeliveryCity = "Istanbul",
            DeliveryPostalCode = "34710",

            DeliveryDate =
                DateOnly.FromDateTime(
                    DateTime.Today.AddDays(1)),

            DeliveryTimeSlot = "10:00-12:00",

            Subtotal =
                (1850m * 3) + (295m * 2),

            DeliveryFee = 0m,

            Total =
                (1850m * 3) + (295m * 2),

            Items =
            [
                new OrderItem
                {
                    ItemType =
                        CartItemType.ShopProduct,

                    ShopProduct =
                        shopProduct,

                    ProductName =
                        shopProduct.Name,

                    UnitPrice =
                        shopProduct.UnitPrice,

                    Quantity = 3,

                    LineTotal =
                        shopProduct.UnitPrice * 3
                },

                new OrderItem
                {
                    ItemType =
                        CartItemType.KitchenMenuItem,

                    KitchenMenuItem =
                        kitchenItem,

                    ProductName =
                        kitchenItem.Name,

                    UnitPrice =
                        kitchenItem.UnitPrice,

                    Quantity = 2,

                    LineTotal =
                        kitchenItem.UnitPrice * 2
                }
            ]
        };

    var payment =
        new PaymentTransaction
        {
            Order = order,

            Provider = "iyzico",

            ConversationId =
                "expired-member-mixed-conversation",

            BasketId =
                order.OrderNumber,

            Token =
                "expired-member-mixed-token",

            PaymentStatus =
                PaymentStatus.Pending,

            Amount =
                order.Total,

            Currency = "TRY",

            CheckoutExpiresAtUtc =
                DateTime.UtcNow.AddMinutes(-5)
        };

    dbContext.AddRange(
        shopProduct,
        kitchenItem,
        order,
        payment);

    await dbContext.SaveChangesAsync();

    var fakeClient =
        new FakeIyzicoCheckoutClient(
            new IyzicoCheckoutRetrieveResult
            {
                Succeeded = true,
                StatusCode = 200,

                ConversationId =
                    payment.ConversationId,

                RawStatus = "success",

                Token =
                    payment.Token,

                BasketId =
                    order.OrderNumber,

                Currency = "TRY",

                PaymentStatus =
                    "PENDING",

                RawResponseJson =
                    "{\"status\":\"success\",\"paymentStatus\":\"PENDING\"}"
            });

    var service =
        new IyzicoPendingPaymentService(
            dbContext,
            fakeClient,
            new KitchenPlanMatchingService(dbContext),
            NullLogger<IyzicoPendingPaymentService>.Instance);

    var processed =
        await service.ProcessExpiredPaymentsAsync();

    Assert.Equal(
        1,
        processed);

    var savedOrder =
        await dbContext.Orders
            .SingleAsync();

    Assert.Equal(
        OrderStatus.Cancelled,
        savedOrder.Status);

    Assert.Equal(
        PaymentStatus.Expired,
        savedOrder.PaymentStatus);

    Assert.NotNull(
        savedOrder.StockRestoredAtUtc);

    var savedPayment =
        await dbContext.PaymentTransactions
            .SingleAsync();

    Assert.Equal(
        PaymentStatus.Expired,
        savedPayment.PaymentStatus);

    Assert.NotNull(
        savedPayment.ExpiredAtUtc);

    Assert.Null(
        savedPayment.CompletedAtUtc);

    Assert.Null(
        savedPayment.FailedAtUtc);

    // Kritik kontrol:
    //
    // Shop başlangıç = 10
    // checkout = 7
    // expiration = 10
    var savedShopProduct =
        await dbContext.ShopProducts
            .SingleAsync();

    Assert.Equal(
        10,
        savedShopProduct.StockQuantity);

    // Kitchen item mevcut kalmalı.
    // Timeout servisi Kitchen entity'sini silmemeli/değiştirmemeli.
    var savedKitchenItem =
        await dbContext.KitchenMenuItems
            .SingleAsync();

    Assert.Equal(
        "Protein Power Bowl",
        savedKitchenItem.Name);
}
[Fact]
public async Task ProcessExpiredPaymentsAsync_WhenRetrieveFails_KeepsOrderPendingAndDoesNotRestoreStock()
{
    await using var dbContext =
        CreateDbContext();

    var product =
        new ShopProduct
        {
            Name = "NO23 Retrieve Failure Hoodie",
            Sku = "EXP-RETRIEVE-FAIL-001",
            Category = "Apparel",
            UnitPrice = 1850m,

            // Baslangic 10, checkout sirasinda
            // 2 adet rezerve edilmis kabul ediyoruz.
            StockQuantity = 8
        };

    var order =
        new Order
        {
            OrderNumber =
                $"NO23-EXP-RETRIEVE-FAIL-{Guid.NewGuid():N}",

            MemberProfileId = null,
            GuestEmail = "retrieve-fail@no23.test",

            Type = OrderType.OneTime,

            Status = OrderStatus.Pending,
            PaymentStatus = PaymentStatus.Pending,

            DeliveryFullName = "Guest Customer",
            DeliveryPhoneNumber = "05551112233",
            DeliveryAddressLine = "Test Sokak No:23",
            DeliveryDistrict = "Kadikoy",
            DeliveryCity = "Istanbul",
            DeliveryPostalCode = "34710",

            DeliveryDate =
                DateOnly.FromDateTime(
                    DateTime.Today.AddDays(1)),

            DeliveryTimeSlot = "10:00-12:00",

            Subtotal = 3700m,
            DeliveryFee = 0m,
            Total = 3700m,

            Items =
            [
                new OrderItem
                {
                    ItemType =
                        CartItemType.ShopProduct,

                    ShopProduct =
                        product,

                    ProductName =
                        product.Name,

                    UnitPrice =
                        product.UnitPrice,

                    Quantity = 2,

                    LineTotal = 3700m
                }
            ]
        };

    var payment =
        new PaymentTransaction
        {
            Order = order,

            Provider = "iyzico",

            ConversationId =
                "retrieve-failure-conversation",

            BasketId =
                order.OrderNumber,

            Token =
                "retrieve-failure-token",

            PaymentStatus =
                PaymentStatus.Pending,

            Amount =
                order.Total,

            Currency = "TRY",

            CheckoutExpiresAtUtc =
                DateTime.UtcNow.AddMinutes(-5)
        };

    dbContext.AddRange(
        product,
        order,
        payment);

    await dbContext.SaveChangesAsync();

    var fakeClient =
        new FakeIyzicoCheckoutClient(
            new IyzicoCheckoutRetrieveResult
            {
                // iyzico'dan güvenilir ödeme sonucu
                // alınamadığını simüle ediyoruz.
                Succeeded = false,

                StatusCode = 500,

                ConversationId =
                    payment.ConversationId,

                RawStatus = "failure",

                Token =
                    payment.Token,

                ErrorCode =
                    "TEMPORARY_ERROR",

                ErrorMessage =
                    "iyzico servisine geçici olarak ulaşılamadı.",

                RawResponseJson =
                    "{\"status\":\"failure\"}"
            });

    var service =
        new IyzicoPendingPaymentService(
            dbContext,
            fakeClient,
            new KitchenPlanMatchingService(dbContext),
            NullLogger<IyzicoPendingPaymentService>.Instance);

    var processed =
        await service.ProcessExpiredPaymentsAsync();

    // İşlem kesin bir sonuca ulaşmadığı için
    // processed sayılmamalı.
    Assert.Equal(
        0,
        processed);

    var savedOrder =
        await dbContext.Orders
            .SingleAsync();

    // En önemli güvenlik kuralı:
    // iyzico cevap vermedi diye siparişi
    // Cancelled yapmıyoruz.
    Assert.Equal(
        OrderStatus.Pending,
        savedOrder.Status);

    Assert.Equal(
        PaymentStatus.Pending,
        savedOrder.PaymentStatus);

    Assert.Null(
        savedOrder.StockRestoredAtUtc);

    var savedPayment =
        await dbContext.PaymentTransactions
            .SingleAsync();

    Assert.Equal(
        PaymentStatus.Pending,
        savedPayment.PaymentStatus);

    Assert.Null(
        savedPayment.ExpiredAtUtc);

    Assert.Null(
        savedPayment.FailedAtUtc);

    Assert.Null(
        savedPayment.CompletedAtUtc);

    // Sorunu log/debug amacıyla DB'de tutuyoruz.
    Assert.NotNull(
        savedPayment.LastError);

    Assert.Contains(
        "TEMPORARY_ERROR",
        savedPayment.LastError);

    // Stok kesinlikle geri verilmemeli.
    //
    // Çünkü ödemenin başarısız olduğunu bilmiyoruz.
    var savedProduct =
        await dbContext.ShopProducts
            .SingleAsync();

    Assert.Equal(
        8,
        savedProduct.StockQuantity);
}

[Fact]
public async Task ProcessExpiredPaymentsAsync_WhenRetrieveReturnsMissingPaymentToken_ExpiresOrderAndRestoresStock()
{
    await using var dbContext =
        CreateDbContext();

    var product =
        new ShopProduct
        {
            Name = "NO23 Missing Token Hoodie",
            Sku = "EXP-MISSING-TOKEN-001",
            Category = "Apparel",
            UnitPrice = 1850m,

            // Baslangic 10, checkout sirasinda
            // 2 adet rezerve edilmis kabul ediyoruz.
            StockQuantity = 8
        };

    var order =
        new Order
        {
            OrderNumber =
                $"NO23-EXP-MISSING-TOKEN-{Guid.NewGuid():N}",

            MemberProfileId = null,
            GuestEmail = "missing-token@no23.test",

            Type = OrderType.OneTime,

            Status = OrderStatus.Pending,
            PaymentStatus = PaymentStatus.Pending,

            DeliveryFullName = "Guest Customer",
            DeliveryPhoneNumber = "05551112233",
            DeliveryAddressLine = "Test Sokak No:23",
            DeliveryDistrict = "Kadikoy",
            DeliveryCity = "Istanbul",
            DeliveryPostalCode = "34710",

            DeliveryDate =
                DateOnly.FromDateTime(
                    DateTime.Today.AddDays(1)),

            DeliveryTimeSlot = "10:00-12:00",

            Subtotal = 3700m,
            DeliveryFee = 0m,
            Total = 3700m,

            Items =
            [
                new OrderItem
                {
                    ItemType =
                        CartItemType.ShopProduct,

                    ShopProduct =
                        product,

                    ProductName =
                        product.Name,

                    UnitPrice =
                        product.UnitPrice,

                    Quantity = 2,

                    LineTotal = 3700m
                }
            ]
        };

    var payment =
        new PaymentTransaction
        {
            Order = order,

            Provider = "iyzico",

            ConversationId =
                "missing-token-conversation",

            BasketId =
                order.OrderNumber,

            Token =
                "missing-token",

            PaymentStatus =
                PaymentStatus.Pending,

            Amount =
                order.Total,

            Currency = "TRY",

            CheckoutExpiresAtUtc =
                DateTime.UtcNow.AddMinutes(-5)
        };

    dbContext.AddRange(
        product,
        order,
        payment);

    await dbContext.SaveChangesAsync();

    var fakeClient =
        new FakeIyzicoCheckoutClient(
            new IyzicoCheckoutRetrieveResult
            {
                Succeeded = false,

                StatusCode = 400,

                ConversationId =
                    payment.ConversationId,

                RawStatus = "failure",

                Token =
                    payment.Token,

                ErrorCode =
                    "5122",

                ErrorMessage =
                    "Gonderilen tokena ait odeme bilgisi bulunamadi",

                RawResponseJson =
                    "{\"status\":\"failure\",\"errorCode\":\"5122\"}"
            });

    var service =
        new IyzicoPendingPaymentService(
            dbContext,
            fakeClient,
            new KitchenPlanMatchingService(dbContext),
            NullLogger<IyzicoPendingPaymentService>.Instance);

    var processed =
        await service.ProcessExpiredPaymentsAsync();

    Assert.Equal(
        1,
        processed);

    var savedOrder =
        await dbContext.Orders
            .SingleAsync();

    Assert.Equal(
        OrderStatus.Cancelled,
        savedOrder.Status);

    Assert.Equal(
        PaymentStatus.Expired,
        savedOrder.PaymentStatus);

    Assert.NotNull(
        savedOrder.StockRestoredAtUtc);

    var savedPayment =
        await dbContext.PaymentTransactions
            .SingleAsync();

    Assert.Equal(
        PaymentStatus.Expired,
        savedPayment.PaymentStatus);

    Assert.NotNull(
        savedPayment.ExpiredAtUtc);

    Assert.Null(
        savedPayment.FailedAtUtc);

    Assert.Null(
        savedPayment.CompletedAtUtc);

    Assert.NotNull(
        savedPayment.LastError);

    Assert.Contains(
        "5122",
        savedPayment.LastError);

    var savedProduct =
        await dbContext.ShopProducts
            .SingleAsync();

    Assert.Equal(
        10,
        savedProduct.StockQuantity);

    var pendingReservedQuantity =
        await dbContext.OrderItems
            .Where(item =>
                item.ShopProductId == savedProduct.Id &&
                item.Order.PaymentStatus == PaymentStatus.Pending &&
                item.Order.Status == OrderStatus.Pending &&
                !item.Order.StockRestoredAtUtc.HasValue)
            .Select(item => (int?)item.Quantity)
            .SumAsync()
        ?? 0;

    Assert.Equal(
        0,
        pendingReservedQuantity);
}

[Fact]
public async Task ProcessExpiredPaymentsAsync_WhenRetrieveReturnsPaymentFailure_CancelsOrderAndRestoresStock()
{
    await using var dbContext =
        CreateDbContext();

    var product =
        new ShopProduct
        {
            Name = "NO23 Insufficient Funds Hoodie",
            Sku = "EXP-PAYMENT-FAIL-001",
            Category = "Apparel",
            UnitPrice = 1850m,

            // Başlangıç stok 10,
            // checkout sırasında 2 adet rezerve edildi.
            StockQuantity = 8
        };

    var order =
        new Order
        {
            OrderNumber =
                $"NO23-EXP-PAYMENT-FAIL-{Guid.NewGuid():N}",

            MemberProfileId = null,
            GuestEmail = "payment-failure@no23.test",

            Type = OrderType.OneTime,

            Status = OrderStatus.Pending,
            PaymentStatus = PaymentStatus.Pending,

            DeliveryFullName = "Guest Customer",
            DeliveryPhoneNumber = "05551112233",
            DeliveryAddressLine = "Test Sokak No:23",
            DeliveryDistrict = "Kadikoy",
            DeliveryCity = "Istanbul",
            DeliveryPostalCode = "34710",

            DeliveryDate =
                DateOnly.FromDateTime(
                    DateTime.Today.AddDays(1)),

            DeliveryTimeSlot = "10:00-12:00",

            Subtotal = 3700m,
            DeliveryFee = 0m,
            Total = 3700m,

            Items =
            [
                new OrderItem
                {
                    ItemType =
                        CartItemType.ShopProduct,

                    ShopProduct =
                        product,

                    ProductName =
                        product.Name,

                    UnitPrice =
                        product.UnitPrice,

                    Quantity = 2,

                    LineTotal = 3700m
                }
            ]
        };

    var payment =
        new PaymentTransaction
        {
            Order = order,

            Provider = "iyzico",

            ConversationId =
                "insufficient-funds-conversation",

            BasketId =
                order.OrderNumber,

            Token =
                "insufficient-funds-token",

            PaymentStatus =
                PaymentStatus.Pending,

            Amount =
                order.Total,

            Currency = "TRY",

            CheckoutExpiresAtUtc =
                DateTime.UtcNow.AddMinutes(-5)
        };

    dbContext.AddRange(
        product,
        order,
        payment);

    await dbContext.SaveChangesAsync();

    var fakeClient =
        new FakeIyzicoCheckoutClient(
            new IyzicoCheckoutRetrieveResult
            {
                // Gerçek sandbox'taki Order 22 gibi:
                // request yapılabiliyor fakat ödeme sonucu failure.
                Succeeded = false,

                StatusCode = 200,

                ConversationId =
                    payment.ConversationId,

                RawStatus =
                    "failure",

                Token =
                    payment.Token,

                BasketId =
                    order.OrderNumber,

                PaymentId =
                    "37176878",

                PaymentStatus =
                    "FAILURE",

                ErrorCode =
                    "10051",

                ErrorGroup =
                    "NOT_SUFFICIENT_FUNDS",

                ErrorMessage =
                    "Kart limiti yetersiz, yetersiz bakiye",

                RawResponseJson =
                    "{\"status\":\"failure\",\"errorCode\":\"10051\",\"paymentStatus\":\"FAILURE\"}"
            });

    var service =
        new IyzicoPendingPaymentService(
            dbContext,
            fakeClient,
            new KitchenPlanMatchingService(dbContext),
            NullLogger<IyzicoPendingPaymentService>.Instance);

    var processed =
        await service.ProcessExpiredPaymentsAsync();

    Assert.Equal(
        1,
        processed);

    var savedOrder =
        await dbContext.Orders
            .SingleAsync();

    Assert.Equal(
        OrderStatus.Cancelled,
        savedOrder.Status);

    Assert.Equal(
        PaymentStatus.Failed,
        savedOrder.PaymentStatus);

    Assert.NotNull(
        savedOrder.StockRestoredAtUtc);

    var savedPayment =
        await dbContext.PaymentTransactions
            .SingleAsync();

    Assert.Equal(
        PaymentStatus.Failed,
        savedPayment.PaymentStatus);

    Assert.NotNull(
        savedPayment.FailedAtUtc);

    Assert.Null(
        savedPayment.ExpiredAtUtc);

    Assert.Null(
        savedPayment.CompletedAtUtc);

    Assert.Equal(
        "37176878",
        savedPayment.PaymentId);

    // Stok:
    // 10 -> checkout ile 8
    // ödeme kesin reddedildi -> 10
    var savedProduct =
        await dbContext.ShopProducts
            .SingleAsync();

    Assert.Equal(
        10,
        savedProduct.StockQuantity);
}

[Fact]
public async Task ProcessExpiredPaymentsAsync_WhenKitchenPaymentActuallySucceeded_ActivatesSubscriptionAndCreatesMealPlan()
{
    await using var dbContext =
        CreateDbContext();

    await SeedKitchenPlanItemsAsync(
        dbContext);

    var scenario =
        await CreatePendingKitchenPaymentAsync(
            dbContext);

    var fakeClient =
        new FakeIyzicoCheckoutClient(
            new IyzicoCheckoutRetrieveResult
            {
                Succeeded = true,
                StatusCode = 200,

                ConversationId =
                    scenario.Payment.ConversationId,

                RawStatus = "success",

                Token =
                    scenario.Payment.Token,

                PaymentId =
                    "kitchen-pending-success-payment",

                PaymentStatus =
                    "SUCCESS",

                BasketId =
                    scenario.Order.OrderNumber,

                Currency = "TRY",

                RawResponseJson =
                    "{\"status\":\"success\",\"paymentStatus\":\"SUCCESS\"}"
            });

    var service =
        new IyzicoPendingPaymentService(
            dbContext,
            fakeClient,
            new KitchenPlanMatchingService(
                dbContext),
            NullLogger<IyzicoPendingPaymentService>.Instance);

    var processed =
        await service.ProcessExpiredPaymentsAsync();

    Assert.Equal(
        1,
        processed);

    var savedOrder =
        await dbContext.Orders
            .SingleAsync();

    Assert.Equal(
        OrderStatus.Confirmed,
        savedOrder.Status);

    Assert.Equal(
        PaymentStatus.Paid,
        savedOrder.PaymentStatus);

    var savedPayment =
        await dbContext.PaymentTransactions
            .SingleAsync();

    Assert.Equal(
        PaymentStatus.Paid,
        savedPayment.PaymentStatus);

    Assert.NotNull(
        savedPayment.CompletedAtUtc);

    var savedSubscription =
        await dbContext.KitchenSubscriptions
            .SingleAsync();

    Assert.Equal(
        KitchenSubscriptionStatus.Active,
        savedSubscription.Status);

    var expectedStart =
        DateOnly.FromDateTime(
            DateTime.Today.AddDays(1));

    Assert.Equal(
        expectedStart,
        savedSubscription.StartsOn);

    Assert.Equal(
        expectedStart.AddDays(4),
        savedSubscription.EndsOn);

    var mealPlan =
        await dbContext.KitchenMealPlans
            .SingleAsync();

    Assert.Equal(
        savedSubscription.Id,
        mealPlan.KitchenSubscriptionId);

    Assert.Equal(
        KitchenMealPlanStatus.Generated,
        mealPlan.Status);
}

[Fact]
public async Task ProcessExpiredPaymentsAsync_WhenKitchenPaymentFailed_MarksSubscriptionPaymentFailed()
{
    await using var dbContext =
        CreateDbContext();

    var scenario =
        await CreatePendingKitchenPaymentAsync(
            dbContext);

    var fakeClient =
        new FakeIyzicoCheckoutClient(
            new IyzicoCheckoutRetrieveResult
            {
                Succeeded = false,
                StatusCode = 200,

                ConversationId =
                    scenario.Payment.ConversationId,

                RawStatus = "failure",

                Token =
                    scenario.Payment.Token,

                PaymentId =
                    "kitchen-pending-failed-payment",

                PaymentStatus =
                    "FAILURE",

                BasketId =
                    scenario.Order.OrderNumber,

                ErrorCode =
                    "10051",

                ErrorGroup =
                    "NOT_SUFFICIENT_FUNDS",

                ErrorMessage =
                    "Kart limiti yetersiz, yetersiz bakiye",

                RawResponseJson =
                    "{\"status\":\"failure\",\"errorCode\":\"10051\",\"paymentStatus\":\"FAILURE\"}"
            });

    var service =
        new IyzicoPendingPaymentService(
            dbContext,
            fakeClient,
            new KitchenPlanMatchingService(
                dbContext),
            NullLogger<IyzicoPendingPaymentService>.Instance);

    var processed =
        await service.ProcessExpiredPaymentsAsync();

    Assert.Equal(
        1,
        processed);

    var savedOrder =
        await dbContext.Orders
            .SingleAsync();

    Assert.Equal(
        OrderStatus.Cancelled,
        savedOrder.Status);

    Assert.Equal(
        PaymentStatus.Failed,
        savedOrder.PaymentStatus);

    var savedPayment =
        await dbContext.PaymentTransactions
            .SingleAsync();

    Assert.Equal(
        PaymentStatus.Failed,
        savedPayment.PaymentStatus);

    Assert.NotNull(
        savedPayment.FailedAtUtc);

    var savedSubscription =
        await dbContext.KitchenSubscriptions
            .SingleAsync();

    Assert.Equal(
        KitchenSubscriptionStatus.PaymentFailed,
        savedSubscription.Status);

    Assert.False(
        await dbContext.KitchenMealPlans
            .AnyAsync());
}

[Fact]
public async Task ProcessExpiredPaymentsAsync_WhenKitchenCheckoutExpired_CancelsSubscriptionWithoutMealPlan()
{
    await using var dbContext =
        CreateDbContext();

    var scenario =
        await CreatePendingKitchenPaymentAsync(
            dbContext);

    var fakeClient =
        new FakeIyzicoCheckoutClient(
            new IyzicoCheckoutRetrieveResult
            {
                Succeeded = true,
                StatusCode = 200,

                ConversationId =
                    scenario.Payment.ConversationId,

                RawStatus = "success",

                Token =
                    scenario.Payment.Token,

                BasketId =
                    scenario.Order.OrderNumber,

                PaymentStatus =
                    "PENDING",

                Currency =
                    "TRY",

                RawResponseJson =
                    "{\"status\":\"success\",\"paymentStatus\":\"PENDING\"}"
            });

    var service =
        new IyzicoPendingPaymentService(
            dbContext,
            fakeClient,
            new KitchenPlanMatchingService(
                dbContext),
            NullLogger<IyzicoPendingPaymentService>.Instance);

    var processed =
        await service.ProcessExpiredPaymentsAsync();

    Assert.Equal(
        1,
        processed);

    var savedOrder =
        await dbContext.Orders
            .SingleAsync();

    Assert.Equal(
        OrderStatus.Cancelled,
        savedOrder.Status);

    Assert.Equal(
        PaymentStatus.Expired,
        savedOrder.PaymentStatus);

    var savedPayment =
        await dbContext.PaymentTransactions
            .SingleAsync();

    Assert.Equal(
        PaymentStatus.Expired,
        savedPayment.PaymentStatus);

    Assert.NotNull(
        savedPayment.ExpiredAtUtc);

    var savedSubscription =
        await dbContext.KitchenSubscriptions
            .SingleAsync();

    Assert.Equal(
        KitchenSubscriptionStatus.Cancelled,
        savedSubscription.Status);

    Assert.False(
        await dbContext.KitchenMealPlans
            .AnyAsync());
}
}
