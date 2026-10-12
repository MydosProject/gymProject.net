using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NO23.Web.Data;
using NO23.Web.Domain.Entities;
using NO23.Web.Domain.Enums;
using NO23.Web.Services;
using NO23.Web.Services.Payments;
using NO23.Web.ViewModels.GuestOrders;
using NO23.Web.ViewModels.Member;
using GuestShopController = NO23.Web.Controllers.ShopController;
using GuestKitchenController = NO23.Web.Controllers.KitchenController;

namespace NO23.Tests;

public class OfflineCheckoutTests
{
    [Theory]
    [InlineData(PaymentMethod.Cash)]
    [InlineData(PaymentMethod.BankTransfer)]
    public async Task GuestShop_ReservesStockAndAwaitsAdminPaymentConfirmation(PaymentMethod method)
    {
        await using var db = CreateDbContext();
        var product = await SeedProductAsync(db);
        var input = GuestInput(method);

        var result = await new CommerceService(db).CreateGuestShopOrderAsync(product.Id, 2, input);

        Assert.True(result.Succeeded);
        Assert.Equal(3, product.StockQuantity);
        var order = await db.Orders.Include(item => item.PaymentInstallments).SingleAsync();
        AssertOfflineOrder(order, method, 200);
        Assert.Empty(await db.PaymentTransactions.ToListAsync());
    }

    [Theory]
    [InlineData(PaymentMethod.Cash)]
    [InlineData(PaymentMethod.BankTransfer)]
    public async Task GuestKitchen_AwaitsAdminPaymentConfirmation(PaymentMethod method)
    {
        await using var db = CreateDbContext();
        var item = new KitchenMenuItem { Name = "Öğün", Ingredients = "", UnitPrice = 125 };
        db.KitchenMenuItems.Add(item);
        await db.SaveChangesAsync();

        var result = await new CommerceService(db).CreateGuestKitchenOrderAsync(item.Id, 2, GuestInput(method));

        Assert.True(result.Succeeded);
        AssertOfflineOrder(await db.Orders.Include(order => order.PaymentInstallments).SingleAsync(), method, 250);
    }

    [Theory]
    [InlineData(PaymentMethod.Cash)]
    [InlineData(PaymentMethod.BankTransfer)]
    [InlineData(PaymentMethod.CreditCard)]
    public async Task MemberCart_IsClearedAtCreationOnlyForOfflineOrders(PaymentMethod method)
    {
        await using var db = CreateDbContext();
        var member = await SeedMemberAsync(db);
        var product = await SeedProductAsync(db);
        var service = new CommerceService(db);
        Assert.True((await service.AddShopProductToCartAsync(member.ApplicationUserId, product.Id, 2)).Succeeded);

        var result = await service.CreateOneTimeOrderFromCartAsync(member.ApplicationUserId, Delivery(), paymentMethod: method);

        Assert.True(result.Succeeded);
        var order = await db.Orders.Include(item => item.PaymentInstallments).SingleAsync();
        Assert.Equal(PaymentStatus.Pending, order.PaymentStatus);
        Assert.Equal(3, product.StockQuantity);
        if (method == PaymentMethod.CreditCard)
        {
            Assert.False(order.IsManualPayment);
            Assert.Empty(order.PaymentInstallments);
            Assert.Single(await db.CartItems.ToListAsync());
        }
        else
        {
            AssertOfflineOrder(order, method, 200);
            Assert.Empty(await db.CartItems.ToListAsync());
            Assert.False((await service.CreateOneTimeOrderFromCartAsync(member.ApplicationUserId, Delivery(), paymentMethod: method)).Succeeded);
            Assert.Single(await db.Orders.ToListAsync());
        }
    }

    [Fact]
    public async Task InvalidPaymentMethod_DoesNotReserveStockOrClearCart()
    {
        await using var db = CreateDbContext();
        var product = await SeedProductAsync(db);
        var member = await SeedMemberAsync(db);
        var service = new CommerceService(db);
        Assert.True((await service.AddShopProductToCartAsync(member.ApplicationUserId, product.Id, 1)).Succeeded);
        var invalid = (PaymentMethod)999;

        Assert.False((await service.CreateGuestShopOrderAsync(product.Id, 1, GuestInput(invalid))).Succeeded);
        Assert.False((await service.CreateOneTimeOrderFromCartAsync(member.ApplicationUserId, Delivery(), paymentMethod: invalid)).Succeeded);

        Assert.Equal(5, product.StockQuantity);
        Assert.Empty(await db.Orders.ToListAsync());
        Assert.Single(await db.CartItems.ToListAsync());
    }

    [Fact]
    public async Task FailedOfflineOrder_PreservesCartAndStock()
    {
        await using var db = CreateDbContext();
        var product = await SeedProductAsync(db);
        var member = await SeedMemberAsync(db);
        var service = new CommerceService(db);
        Assert.True((await service.AddShopProductToCartAsync(member.ApplicationUserId, product.Id, 2)).Succeeded);
        product.StockQuantity = 1;
        await db.SaveChangesAsync();

        var result = await service.CreateOneTimeOrderFromCartAsync(member.ApplicationUserId, Delivery(), paymentMethod: PaymentMethod.Cash);

        Assert.False(result.Succeeded);
        Assert.Equal(1, product.StockQuantity);
        Assert.Single(await db.CartItems.ToListAsync());
        Assert.Empty(await db.Orders.ToListAsync());
    }

    [Theory]
    [InlineData(PaymentMethod.Cash)]
    [InlineData(PaymentMethod.BankTransfer)]
    public async Task KitchenPackage_IncludesDeliveryFeeAndReusesSamePendingOrder(PaymentMethod method)
    {
        await using var db = CreateDbContext();
        var member = await SeedMemberAsync(db);
        var subscription = new KitchenSubscription
        {
            MemberProfile = member, PackageNameSnapshot = "5 günlük Kitchen", PackageDaysSnapshot = 5,
            PackagePriceSnapshot = 1000, DailyDeliveryFeeSnapshot = 50,
            KitchenSubscriptionPackage = new KitchenSubscriptionPackage { Name = "5 gün", Days = 5 }
        };
        db.KitchenSubscriptions.Add(subscription);
        await db.SaveChangesAsync();
        var service = new CommerceService(db);
        var result = await service.CreateKitchenPackageOrderAsync(member.ApplicationUserId, subscription.Id, Delivery(), method);
        var repeated = await service.CreateKitchenPackageOrderAsync(member.ApplicationUserId, subscription.Id, Delivery(), method);

        Assert.True(result.Succeeded);
        Assert.Equal(result.EntityId, repeated.EntityId);
        AssertOfflineOrder(await db.Orders.Include(order => order.PaymentInstallments).SingleAsync(), method, 1250);
        Assert.Equal(KitchenSubscriptionStatus.PendingPayment, subscription.Status);
        var changedMethod = method == PaymentMethod.Cash ? PaymentMethod.BankTransfer : PaymentMethod.Cash;
        Assert.False((await service.CreateKitchenPackageOrderAsync(member.ApplicationUserId, subscription.Id, Delivery(), changedMethod)).Succeeded);
        Assert.Single(await db.Orders.ToListAsync());
    }

    [Theory]
    [InlineData(PaymentMethod.Cash)]
    [InlineData(PaymentMethod.BankTransfer)]
    public async Task PublicCheckout_AllowsOfflineMethodsWhenCardProviderDisabled(PaymentMethod method)
    {
        await using var db = CreateDbContext();
        var product = await SeedProductAsync(db);
        var menuItem = new KitchenMenuItem { Name = "Öğün", Ingredients = "", UnitPrice = 125 };
        db.KitchenMenuItems.Add(menuItem);
        await db.SaveChangesAsync();
        var options = Options.Create(new IyzicoOptions { Enabled = false });
        var fakeClient = new FakeIyzicoCheckoutClient(new IyzicoCheckoutInitializeResult { Succeeded = false });
        var paymentService = new IyzicoPaymentService(db, fakeClient, new KitchenPlanMatchingService(db), options,
            NullLogger<IyzicoPaymentService>.Instance);
        var commerce = new CommerceService(db);
        var pickup = Options.Create(new ClubPickupOptions());
        var shop = new GuestShopController(db, commerce, paymentService, options, pickup)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
        var kitchen = new GuestKitchenController(db, commerce, paymentService, options, pickup,
            new CalorieCalculatorService(), NullLogger<GuestKitchenController>.Instance)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        Assert.IsType<RedirectToActionResult>(await shop.PlaceOrder(product.Id, GuestInput(method)));
        Assert.IsType<RedirectToActionResult>(await kitchen.PlaceOrder(menuItem.Id, GuestInput(method)));
        Assert.Null(fakeClient.LastInitializeRequest);
        Assert.Equal(2, await db.Orders.CountAsync());
        Assert.All(await db.Orders.ToListAsync(), order => Assert.Equal(PaymentStatus.Pending, order.PaymentStatus));
    }

    [Fact]
    public async Task PublicCheckout_RejectsCardWhenProviderDisabledBeforeReservingStock()
    {
        await using var db = CreateDbContext();
        var product = await SeedProductAsync(db);
        var options = Options.Create(new IyzicoOptions { Enabled = false });
        var shop = new GuestShopController(db, new CommerceService(db), null!, options, Options.Create(new ClubPickupOptions()));

        Assert.IsType<ViewResult>(await shop.PlaceOrder(product.Id, GuestInput(PaymentMethod.CreditCard)));
        Assert.Equal(5, product.StockQuantity);
        Assert.Empty(await db.Orders.ToListAsync());
    }

    [Fact]
    public void CheckoutModels_RejectUnknownPaymentMethods()
    {
        object[] models = [GuestInput((PaymentMethod)999), new CheckoutInputViewModel { PaymentMethod = (PaymentMethod)999 },
            new KitchenCheckoutViewModel { PaymentMethod = (PaymentMethod)999 }];
        foreach (var model in models)
        {
            var errors = new List<ValidationResult>();
            Validator.TryValidateObject(model, new ValidationContext(model), errors, true);
            Assert.Contains(errors, error => error.MemberNames.Contains("PaymentMethod"));
        }
    }

    private static void AssertOfflineOrder(Order order, PaymentMethod method, decimal expectedTotal)
    {
        Assert.Equal(method, order.PaymentMethod);
        Assert.True(order.IsManualPayment);
        Assert.Equal(OrderStatus.Pending, order.Status);
        Assert.Equal(PaymentStatus.Pending, order.PaymentStatus);
        Assert.Equal(expectedTotal, order.Total);
        var installment = Assert.Single(order.PaymentInstallments);
        Assert.Equal(expectedTotal, installment.Amount);
        Assert.Null(installment.PaidOn);
    }

    private static GuestOrderInputViewModel GuestInput(PaymentMethod method) => new()
    {
        PaymentMethod = method, FullName = "Test Üye", Email = "test@example.com", PhoneNumber = "05555555555",
        AddressLine = "Test adresi", City = "İstanbul", District = "Kadıköy", DeliveryTimeSlot = "10:00-13:00"
    };

    private static DeliveryDetails Delivery() => new()
    {
        FullName = "Test Üye", PhoneNumber = "05555555555", AddressLine = "Test adresi", City = "İstanbul", District = "Kadıköy"
    };

    private static async Task<ShopProduct> SeedProductAsync(ApplicationDbContext db)
    {
        var product = new ShopProduct { Name = "Test ürün", Sku = $"TEST-{Guid.NewGuid():N}", Category = "Shop", UnitPrice = 100, StockQuantity = 5 };
        db.ShopProducts.Add(product);
        await db.SaveChangesAsync();
        return product;
    }

    private static async Task<MemberProfile> SeedMemberAsync(ApplicationDbContext db)
    {
        var member = new MemberProfile
        {
            ApplicationUser = new ApplicationUser { UserName = "test@example.com", Email = "test@example.com" },
            MembershipPackage = new MembershipPackage { Code = MembershipPackageCode.Pro, Name = "Pro", Audience = "Test", Description = "Test" }
        };
        db.MemberProfiles.Add(member);
        await db.SaveChangesAsync();
        return member;
    }

    private static ApplicationDbContext CreateDbContext() => new(new DbContextOptionsBuilder<ApplicationDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
}
