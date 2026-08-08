using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NO23.Web.Data;
using NO23.Web.Domain.Entities;
using NO23.Web.Domain.Enums;
using NO23.Web.Services.Payments;
using Microsoft.AspNetCore.WebUtilities;

namespace NO23.Tests;

public class IyzicoPaymentServiceTests
{
    [Fact]
    public async Task InitializeAsync_MemberPaymentSuccess_SavesTokenAndUrl_ClearsCart_AndKeepsReservedStock()
    {
        await using var dbContext = CreateDbContext();

        var scenario =
            await SeedMemberPaymentScenarioAsync(dbContext);

        var fakeClient =
            new FakeIyzicoCheckoutClient(
                new IyzicoCheckoutInitializeResult
                {
                    Succeeded = true,
                    StatusCode = 200,
                    ConversationId =
                        "conversation-success",
                    RawStatus = "success",
                    Token = "checkout-token-123",
                    PaymentPageUrl =
                        "https://sandbox-payment.example/checkout-token-123",
                    RawResponseJson =
                        "{\"status\":\"success\"}"
                });

        var service =
            CreateService(dbContext, fakeClient);

        var result =
            await service.InitializeAsync(
                scenario.Order.Id,
                "127.0.0.1");

        // Initialize başarılı mı?
        Assert.True(result.Succeeded);

        Assert.Equal(
            scenario.Order.Id,
            result.OrderId);

        Assert.Equal(
            "https://sandbox-payment.example/checkout-token-123",
            result.RedirectUrl);

        // PaymentTransaction oluşmuş mu?
        var payment =
            await dbContext.PaymentTransactions
                .SingleAsync();

        // Initialize sadece ödeme ekranını açar.
        // Henüz callback gelmediği için ödeme Pending kalır.
        Assert.Equal(
            PaymentStatus.Pending,
            payment.PaymentStatus);

        // iyzico token kaydedilmiş mi?
        Assert.Equal(
            "checkout-token-123",
            payment.Token);

        // iyzico ödeme URL'i kaydedilmiş mi?
        Assert.Equal(
            "https://sandbox-payment.example/checkout-token-123",
            payment.PaymentPageUrl);

        Assert.Equal(
            "success",
            payment.RawStatus);

        Assert.Equal(
            "{\"status\":\"success\"}",
            payment.RawInitializeResponseJson);

        // Başarılı initialize sonrası üye sepeti silinmeli.
        Assert.False(
            await dbContext.ShoppingCarts.AnyAsync());

        Assert.False(
            await dbContext.CartItems.AnyAsync());

        // Başlangıç stoğu 10 idi.
        // Sipariş oluşturulurken 2 adet rezerve edildi
        // ve test senaryosunda stok 8 olarak başlıyor.
        //
        // Initialize başarılıysa stok GERİ VERİLMEMELİ.
        var product =
            await dbContext.ShopProducts.SingleAsync();

        Assert.Equal(
            8,
            product.StockQuantity);

        Assert.Null(
            scenario.Order.StockRestoredAtUtc);

        // Fake client gerçekten çağrılmış mı?
        Assert.NotNull(
            fakeClient.LastInitializeRequest);

        Assert.Equal(
            scenario.Order.OrderNumber,
            fakeClient.LastInitializeRequest!.BasketId);

        Assert.Equal(
            scenario.Order.Total,
            fakeClient.LastInitializeRequest.PaidPrice);
    }

    [Fact]
    public async Task InitializeAsync_MemberReturnUrl_PassesCallbackUrlWithMemberOrdersToCheckoutClient()
    {
        await using var dbContext =
            CreateDbContext();

        var scenario =
            await SeedMemberPaymentScenarioAsync(
                dbContext);

        var fakeClient =
            new FakeIyzicoCheckoutClient(
                new IyzicoCheckoutInitializeResult
                {
                    Succeeded = true,
                    StatusCode = 200,
                    ConversationId =
                        "conversation-member-return-url",
                    RawStatus = "success",
                    Token =
                        "checkout-token-member-return-url",
                    PaymentPageUrl =
                        "https://sandbox-payment.example/member-return-url",
                    RawResponseJson =
                        "{\"status\":\"success\"}"
                });

        var service =
            CreateService(
                dbContext,
                fakeClient);

        const string memberReturnUrl =
            "https://localhost:7220/Member/Orders";

        var result =
            await service.InitializeAsync(
                scenario.Order.Id,
                "127.0.0.1",
                memberReturnUrl);

        Assert.True(result.Succeeded);

        Assert.NotNull(
            fakeClient.LastInitializeRequest);

        var initializeRequest =
            fakeClient.LastInitializeRequest!;

        // Member ödeme bilgileri değişmemeli.
        Assert.True(
            scenario.Order.MemberProfileId.HasValue);

        Assert.Equal(
            $"MEMBER-{scenario.Order.MemberProfileId.Value}",
            initializeRequest.Buyer.Id);

        Assert.Equal(
            "member@no23.test",
            initializeRequest.Buyer.Email);

        // iyzico'ya gönderilen callback URL boş olmamalı.
        Assert.False(
            string.IsNullOrWhiteSpace(
                initializeRequest.CallbackUrl));

        var callbackUri =
            new Uri(
                initializeRequest.CallbackUrl);

        Assert.Equal(
            "/payment/iyzico/callback",
            callbackUri.AbsolutePath);

        // Callback içinde Member/Orders dönüş adresi korunmalı.
        var query =
            QueryHelpers.ParseQuery(
                callbackUri.Query);

        Assert.True(
            query.TryGetValue(
                "returnUrl",
                out var returnUrl));

        Assert.Equal(
            memberReturnUrl,
            returnUrl.ToString());
    }

    [Fact]
    public async Task InitializeAsync_MemberPaymentFailure_CancelsOrder_MarksPaymentFailed_RestoresStock_AndKeepsCart()
    {
        await using var dbContext = CreateDbContext();

        var scenario =
            await SeedMemberPaymentScenarioAsync(dbContext);

        var fakeClient =
            new FakeIyzicoCheckoutClient(
                new IyzicoCheckoutInitializeResult
                {
                    Succeeded = false,
                    StatusCode = 400,
                    ConversationId =
                        "conversation-failed",
                    RawStatus = "failure",
                    ErrorCode = "10051",
                    ErrorMessage =
                        "Ödeme formu başlatılamadı.",
                    ErrorGroup = "validation",
                    RawResponseJson =
                        "{\"status\":\"failure\"}"
                });

        var service =
            CreateService(dbContext, fakeClient);

        var result =
            await service.InitializeAsync(
                scenario.Order.Id,
                "127.0.0.1");

        // Initialize başarısız olmalı.
        Assert.False(result.Succeeded);

        Assert.Equal(
            scenario.Order.Id,
            result.OrderId);

        // Sipariş iptal edilmeli.
        var order =
            await dbContext.Orders.SingleAsync();

        Assert.Equal(
            OrderStatus.Cancelled,
            order.Status);

        // Siparişin payment durumu Failed olmalı.
        Assert.Equal(
            PaymentStatus.Failed,
            order.PaymentStatus);

        // Stok iadesi yapıldığı işaretlenmeli.
        Assert.NotNull(
            order.StockRestoredAtUtc);

        // PaymentTransaction da Failed olmalı.
        var payment =
            await dbContext.PaymentTransactions
                .SingleAsync();

        Assert.Equal(
            PaymentStatus.Failed,
            payment.PaymentStatus);

        Assert.Equal(
            "failure",
            payment.RawStatus);

        Assert.NotNull(
            payment.FailedAtUtc);

        Assert.Contains(
            "10051",
            payment.LastError);

        Assert.Equal(
            "{\"status\":\"failure\"}",
            payment.RawInitializeResponseJson);

        // Başlangıçta sipariş için stok 10 -> 8 olmuştu.
        // Failure sonrası 2 adet geri verilerek tekrar 10 olmalı.
        var product =
            await dbContext.ShopProducts
                .SingleAsync();

        Assert.Equal(
            10,
            product.StockQuantity);

        // FAILURE olduğunda sepet KORUNMALI.
        var cart =
            await dbContext.ShoppingCarts
                .Include(item => item.Items)
                .SingleAsync();

        Assert.Single(cart.Items);

        Assert.Equal(
            2,
            cart.Items.Single().Quantity);

        Assert.Equal(
            scenario.Product.Id,
            cart.Items.Single().ShopProductId);
    }

    [Fact]
public async Task InitializeAsync_GuestPaymentSuccess_UsesGuestBuyerAndCreatesPaymentTransaction()
{
    await using var dbContext =
        CreateDbContext();

    var product =
        new ShopProduct
        {
            Name =
                "NO23 Guest Test Product",

            Sku =
                "GUEST-TEST-001",

            Category =
                "Equipment",

            UnitPrice =
                150m,

            StockQuantity =
                4
        };

    var order =
        new Order
        {
            OrderNumber =
                $"NO23-GUEST-{Guid.NewGuid():N}",

            // Guest sipariş:
            // MemberProfile bağlı DEĞİL.
            MemberProfileId =
                null,

            GuestEmail =
                "guest@no23.test",

            Type =
                OrderType.OneTime,

            Status =
                OrderStatus.Pending,

            PaymentStatus =
                PaymentStatus.Pending,

            DeliveryFullName =
                "Guest Customer",

            DeliveryPhoneNumber =
                "05551112233",

            DeliveryAddressLine =
                "Guest Sokak No:23",

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
                300m,

            DeliveryFee =
                0m,

            Total =
                300m,

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
                        2,

                    LineTotal =
                        300m
                }
            ]
        };

    dbContext.AddRange(
        product,
        order);

    await dbContext.SaveChangesAsync();

    var fakeClient =
        new FakeIyzicoCheckoutClient(
            new IyzicoCheckoutInitializeResult
            {
                Succeeded =
                    true,

                StatusCode =
                    200,

                ConversationId =
                    "conversation-guest-success",

                RawStatus =
                    "success",

                Token =
                    "checkout-token-guest",

                PaymentPageUrl =
                    "https://sandbox-payment.example/guest",

                RawResponseJson =
                    "{\"status\":\"success\"}"
            });

            var service =
                CreateService(
                    dbContext,
                    fakeClient);

            var result =
                await service.InitializeAsync(
                    order.Id,
                    "127.0.0.1");

            // Guest ödeme initialize başarılı olmalı.
            Assert.True(
                result.Succeeded);

            Assert.Equal(
                order.Id,
                result.OrderId);

            Assert.Equal(
                "https://sandbox-payment.example/guest",
                result.RedirectUrl);

            // iyzico request gerçekten oluşturulmuş mu?
            Assert.NotNull(
                fakeClient.LastInitializeRequest);

            var request =
                fakeClient.LastInitializeRequest!;

            // Guest sipariş member'a bağlı olmamalı.
            Assert.Null(
                order.MemberProfileId);

            // iyzico Buyer.Id MEMBER değil GUEST olmalı.
            Assert.Equal(
                $"GUEST-{order.Id}",
                request.Buyer.Id);

            // E-posta ApplicationUser'dan değil
            // Order.GuestEmail'den gelmeli.
            Assert.Equal(
                "guest@no23.test",
                request.Buyer.Email);

            // iyzico basket bilgileri siparişle eşleşmeli.
            Assert.Equal(
                order.OrderNumber,
                request.BasketId);

            Assert.Equal(
                order.Subtotal,
                request.Price);

            Assert.Equal(
                order.Total,
                request.PaidPrice);

            // Guest ödeme için PaymentTransaction oluşmalı.
            var payment =
                await dbContext.PaymentTransactions
                    .SingleAsync();

            Assert.Equal(
                order.Id,
                payment.OrderId);

            Assert.Equal(
                "iyzico",
                payment.Provider);

            Assert.Equal(
                PaymentStatus.Pending,
                payment.PaymentStatus);

            Assert.Equal(
                order.Total,
                payment.Amount);

            Assert.Equal(
                "TRY",
                payment.Currency);

            Assert.Equal(
                "checkout-token-guest",
                payment.Token);

            Assert.Equal(
                "https://sandbox-payment.example/guest",
                payment.PaymentPageUrl);
        }

    [Fact]
    public async Task InitializeAsync_GuestShopPaymentFailure_CancelsOrder_MarksPaymentFailed_AndRestoresStock()
    {
    await using var dbContext =
        CreateDbContext();

    // Gerçekte ürün stoğunun başlangıçta 10 olduğunu varsayıyoruz.
    // Guest sipariş oluşturulduğunda 2 adet rezerve edilmiş:
    // 10 -> 8
    var product =
        new ShopProduct
        {
            Name =
                "NO23 Guest Training Gloves",

            Sku =
                "GUEST-STOCK-TEST-001",

            Category =
                "Equipment",

            UnitPrice =
                100m,

            StockQuantity =
                8
        };

    var order =
        new Order
        {
            OrderNumber =
                $"NO23-GUEST-STOCK-{Guid.NewGuid():N}",

            // Bu bir GUEST sipariş.
            MemberProfileId =
                null,

            GuestEmail =
                "guest-stock@no23.test",

            Type =
                OrderType.OneTime,

            Status =
                OrderStatus.Pending,

            PaymentStatus =
                PaymentStatus.Pending,

            DeliveryFullName =
                "Guest Customer",

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
                200m,

            DeliveryFee =
                0m,

            Total =
                200m,

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
                        2,

                    LineTotal =
                        200m
                }
            ]
        };

    dbContext.AddRange(
        product,
        order);

    await dbContext.SaveChangesAsync();

    // iyzico initialize işlemini bilinçli olarak başarısız yapıyoruz.
    var fakeClient =
        new FakeIyzicoCheckoutClient(
            new IyzicoCheckoutInitializeResult
            {
                Succeeded =
                    false,

                StatusCode =
                    400,

                ConversationId =
                    "conversation-guest-stock-failure",

                RawStatus =
                    "failure",

                ErrorCode =
                    "10051",

                ErrorMessage =
                    "Ödeme formu başlatılamadı.",

                ErrorGroup =
                    "validation",

                RawResponseJson =
                    "{\"status\":\"failure\"}"
            });

    var service =
        CreateService(
            dbContext,
            fakeClient);

    var result =
        await service.InitializeAsync(
            order.Id,
            "127.0.0.1");

    // 1 - Ödeme initialize başarısız olmalı.
    Assert.False(
        result.Succeeded);

    Assert.Equal(
        order.Id,
        result.OrderId);

    // 2 - Sipariş iptal edilmiş olmalı.
    var savedOrder =
        await dbContext.Orders
            .Include(item => item.Items)
                .ThenInclude(item => item.ShopProduct)
            .SingleAsync();

    Assert.Equal(
        OrderStatus.Cancelled,
        savedOrder.Status);

    // 3 - Sipariş payment durumu Failed olmalı.
    Assert.Equal(
        PaymentStatus.Failed,
        savedOrder.PaymentStatus);

    // 4 - Stok iadesinin yapıldığı işaretlenmeli.
    Assert.NotNull(
        savedOrder.StockRestoredAtUtc);

    // 5 - PaymentTransaction oluşmuş ve Failed olmuş olmalı.
    var payment =
        await dbContext.PaymentTransactions
            .SingleAsync();

    Assert.Equal(
        order.Id,
        payment.OrderId);

    Assert.Equal(
        PaymentStatus.Failed,
        payment.PaymentStatus);

    Assert.Equal(
        "failure",
        payment.RawStatus);

    Assert.NotNull(
        payment.FailedAtUtc);

    Assert.Contains(
        "10051",
        payment.LastError);

    // 6 - EN KRİTİK KONTROL:
    //
    // Sipariş oluşturulduğunda:
    // 10 -> 8
    //
    // iyzico initialize başarısız olduğunda:
    // 8 -> 10
    var savedProduct =
        await dbContext.ShopProducts
            .SingleAsync();

    Assert.Equal(
        10,
        savedProduct.StockQuantity);

    // 7 - Guest siparişte member sepeti olmamalı.
    Assert.False(
        await dbContext.ShoppingCarts.AnyAsync());

    Assert.False(
        await dbContext.CartItems.AnyAsync());
    }

    private static IyzicoPaymentService CreateService(
        ApplicationDbContext dbContext,
        IIyzicoCheckoutClient checkoutClient)
    {
        var options =
        Options.Create(
        new IyzicoOptions
        {
            Currency = "TRY",

            CallbackUrl =
                "https://localhost:7220/payment/iyzico/callback",

            SandboxBuyerIdentityNumber =
                "74300864791"
        });


        return new IyzicoPaymentService(
            dbContext,
            checkoutClient,
            options,
            NullLogger<IyzicoPaymentService>.Instance);
    }

    private static ApplicationDbContext CreateDbContext()
    {
        var options =
            new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(
                    Guid.NewGuid().ToString())
                .Options;

        return new ApplicationDbContext(options);
    }

    private static async Task<MemberPaymentScenario>
        SeedMemberPaymentScenarioAsync(
            ApplicationDbContext dbContext)
    {
        var user = new ApplicationUser
        {
            Id = "member-user-1",

            UserName =
                "member@no23.test",

            NormalizedUserName =
                "MEMBER@NO23.TEST",

            Email =
                "member@no23.test",

            NormalizedEmail =
                "MEMBER@NO23.TEST",

            CreatedAtUtc =
                DateTime.UtcNow.AddMonths(-1),

            LastLoginAtUtc =
                DateTime.UtcNow.AddMinutes(-5)
        };

        var package = new MembershipPackage
        {
            Code =
                MembershipPackageCode.Start,

            Name = "START",

            Audience = "Test",

            Description = "Test package"
        };

        var member = new MemberProfile
        {
            ApplicationUser = user,

            ApplicationUserId = user.Id,

            MembershipPackage = package
        };

        // Gerçekte ürün stoğu 10 kabul ediyoruz.
        //
        // Sipariş oluşturulduğunda 2 adet rezerve edildiği
        // için ödeme initialize aşamasında stok 8'dir.
        var product = new ShopProduct
        {
            Name =
                "NO23 Training Gloves",

            Sku =
                "TEST-GLOVE-001",

            Category =
                "Equipment",

            UnitPrice =
                100m,

            StockQuantity =
                8
        };

        // Ödeme başlamadan önce sepet hâlâ duruyor.
        var cart = new ShoppingCart
        {
            MemberProfile = member,

            Items =
            [
                new CartItem
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
                        2
                }
            ]
        };

        // Sipariş oluşturulmuş ve stok rezerve edilmiş durumda.
        var order = new Order
        {
            OrderNumber =
                $"NO23-TEST-{Guid.NewGuid():N}",

            MemberProfile =
                member,

            Type =
                OrderType.OneTime,

            Status =
                OrderStatus.Pending,

            PaymentStatus =
                PaymentStatus.Pending,

            DeliveryFullName =
                "Sena Test",

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
                200m,

            DeliveryFee =
                0m,

            Total =
                200m,

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
                        2,

                    LineTotal =
                        200m
                }
            ]
        };

        dbContext.AddRange(
            user,
            package,
            member,
            product,
            cart,
            order);

        await dbContext.SaveChangesAsync();

        return new MemberPaymentScenario(
            order,
            product);
    }

    private sealed record MemberPaymentScenario(
        Order Order,
        ShopProduct Product);


    [Fact]
public async Task HandleCallbackAsync_PaymentSuccess_MarksPaymentPaid_ConfirmsOrder_AndKeepsReservedStock()
{
    await using var dbContext =
        CreateDbContext();

    var scenario =
        await SeedMemberPaymentScenarioAsync(
            dbContext);

    const string token =
        "callback-token-success";

    const string conversationId =
        "conversation-callback-success";

    var paymentTransaction =
        new PaymentTransaction
        {
            OrderId =
                scenario.Order.Id,

            Provider =
                "iyzico",

            ConversationId =
                conversationId,

            BasketId =
                scenario.Order.OrderNumber,

            Token =
                token,

            PaymentPageUrl =
                "https://sandbox-payment.example/payment",

            PaymentStatus =
                PaymentStatus.Pending,

            Amount =
                scenario.Order.Total,

            Currency =
                "TRY"
        };

    dbContext.PaymentTransactions.Add(
        paymentTransaction);

    await dbContext.SaveChangesAsync();

    var fakeClient =
        new FakeIyzicoCheckoutClient(
            new IyzicoCheckoutRetrieveResult
            {
                Succeeded = true,

                StatusCode = 200,

                ConversationId =
                    conversationId,

                RawStatus =
                    "success",

                Token =
                    token,

                PaymentId =
                    "payment-123",

                PaymentStatus =
                    "SUCCESS",

                FraudStatus =
                    1,

                BasketId =
                    scenario.Order.OrderNumber,

                Price =
                    "200.00",

                PaidPrice =
                    "200.00",

                Currency =
                    "TRY",

                RawResponseJson =
                    "{\"status\":\"success\",\"paymentStatus\":\"SUCCESS\"}"
            });

    var service =
        CreateService(
            dbContext,
            fakeClient);

    var result =
        await service.HandleCallbackAsync(
            token);

    // Callback işlemi başarılı olmalı.
    Assert.True(
        result.Succeeded);

    Assert.Equal(
        scenario.Order.Id,
        result.OrderId);

    // PaymentTransaction Paid olmalı.
    var payment =
        await dbContext.PaymentTransactions
            .SingleAsync();

    Assert.Equal(
        PaymentStatus.Paid,
        payment.PaymentStatus);

    Assert.Equal(
        "payment-123",
        payment.PaymentId);

    Assert.Equal(
        1,
        payment.FraudStatus);

    Assert.NotNull(
        payment.CallbackReceivedAtUtc);

    Assert.NotNull(
        payment.CompletedAtUtc);

    Assert.Null(
        payment.FailedAtUtc);

    Assert.Null(
        payment.LastError);

    Assert.Equal(
        "{\"status\":\"success\",\"paymentStatus\":\"SUCCESS\"}",
        payment.RawRetrieveResponseJson);

    // Sipariş ödeme durumu Paid olmalı.
    var order =
        await dbContext.Orders
            .SingleAsync();

    Assert.Equal(
        PaymentStatus.Paid,
        order.PaymentStatus);

    // Başarılı ödeme sonrası sipariş onaylanmalı.
    Assert.Equal(
        OrderStatus.Confirmed,
        order.Status);

    // Stok sipariş oluşturulurken 10 -> 8 olmuştu.
    // Başarılı ödeme sonrası aynı kalmalı.
    var product =
        await dbContext.ShopProducts
            .SingleAsync();

    Assert.Equal(
        8,
        product.StockQuantity);

    // Stok geri verilmemiş olmalı.
    Assert.Null(
        order.StockRestoredAtUtc);

    // Fake Retrieve gerçekten doğru değerlerle çağrılmış mı?
    Assert.Equal(
        conversationId,
        fakeClient.LastRetrieveConversationId);

    Assert.Equal(
        token,
        fakeClient.LastRetrieveToken);
}
[Fact]
public async Task HandleCallbackAsync_PaymentFailure_MarksPaymentFailed_CancelsOrder_AndRestoresStock()
{
    await using var dbContext =
        CreateDbContext();

    var scenario =
        await SeedMemberPaymentScenarioAsync(
            dbContext);

    const string token =
        "callback-token-failure";

    const string conversationId =
        "conversation-callback-failure";

    var paymentTransaction =
        new PaymentTransaction
        {
            OrderId =
                scenario.Order.Id,

            Provider =
                "iyzico",

            ConversationId =
                conversationId,

            BasketId =
                scenario.Order.OrderNumber,

            Token =
                token,

            PaymentPageUrl =
                "https://sandbox-payment.example/payment",

            PaymentStatus =
                PaymentStatus.Pending,

            Amount =
                scenario.Order.Total,

            Currency =
                "TRY"
        };

    dbContext.PaymentTransactions.Add(
        paymentTransaction);

    await dbContext.SaveChangesAsync();

    var fakeClient =
        new FakeIyzicoCheckoutClient(
            new IyzicoCheckoutRetrieveResult
            {
                // Retrieve çağrısı teknik olarak başarılı.
                //
                // Ancak ödeme sonucu FAILURE.
                Succeeded = true,

                StatusCode = 200,

                ConversationId =
                    conversationId,

                RawStatus =
                    "success",

                Token =
                    token,

                PaymentId =
                    "payment-failed-123",

                PaymentStatus =
                    "FAILURE",

                FraudStatus =
                    -1,

                BasketId =
                    scenario.Order.OrderNumber,

                Price =
                    "200.00",

                PaidPrice =
                    "200.00",

                Currency =
                    "TRY",

                RawResponseJson =
                    "{\"status\":\"success\",\"paymentStatus\":\"FAILURE\"}"
            });

    var service =
        CreateService(
            dbContext,
            fakeClient);

    var result =
        await service.HandleCallbackAsync(
            token);

    // Ödeme başarısız olduğu için callback sonucu false.
    Assert.False(
        result.Succeeded);

    Assert.Equal(
        scenario.Order.Id,
        result.OrderId);

    // PaymentTransaction Failed olmalı.
    var payment =
        await dbContext.PaymentTransactions
            .SingleAsync();

    Assert.Equal(
        PaymentStatus.Failed,
        payment.PaymentStatus);

    Assert.Equal(
        "payment-failed-123",
        payment.PaymentId);

    Assert.NotNull(
        payment.CallbackReceivedAtUtc);

    Assert.NotNull(
        payment.FailedAtUtc);

    Assert.Null(
        payment.CompletedAtUtc);

    Assert.NotNull(
        payment.LastError);

    Assert.Contains(
        "FAILURE",
        payment.LastError);

    // Sipariş başarısız ödeme nedeniyle iptal edilmeli.
    var order =
        await dbContext.Orders
            .SingleAsync();

    Assert.Equal(
        PaymentStatus.Failed,
        order.PaymentStatus);

    Assert.Equal(
        OrderStatus.Cancelled,
        order.Status);

    // Sipariş için daha önce:
    //
    // stok 10 -> 8
    //
    // olmuştu.
    //
    // Callback FAILURE sonrası 2 adet geri verilmeli.
    var product =
        await dbContext.ShopProducts
            .SingleAsync();

    Assert.Equal(
        10,
        product.StockQuantity);

    // Stok iadesinin yapıldığı kaydedilmeli.
    Assert.NotNull(
        order.StockRestoredAtUtc);

    Assert.Equal(
        conversationId,
        fakeClient.LastRetrieveConversationId);

    Assert.Equal(
        token,
        fakeClient.LastRetrieveToken);
}
}