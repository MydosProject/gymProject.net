using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NO23.Web.Data;
using NO23.Web.Domain.Entities;
using NO23.Web.Domain.Enums;
using NO23.Web.Services;
using NO23.Web.Services.Payments;

namespace NO23.Tests;

public class MembershipRenewalServiceTests
{
    [Fact]
    public async Task CreateOrderAsync_UsesServerPrice_AndPaidActivationIsIdempotent()
    {
        await using var db = CreateDb();
        var profile = Seed(db);
        await db.SaveChangesAsync();
        var service = new MembershipRenewalService(db);

        var (orderId, error) = await service.CreateOrderAsync("member-1", 11,
            "Test Member", "05551234567", "Test sokak 1", "Kadıköy", "İstanbul");

        Assert.Null(error);
        var order = await db.Orders.Include(x => x.Items).SingleAsync();
        Assert.Equal(order.Id, orderId);
        Assert.Equal(OrderType.MembershipRenewal, order.Type);
        Assert.Equal(72000m, order.Total);
        Assert.Equal(CartItemType.MembershipPackage, order.Items.Single().ItemType);
        Assert.Equal(72000m, order.Items.Single().LineTotal);
        Assert.Equal(3, profile.RemainingClassCredits);

        order.PaymentStatus = PaymentStatus.Paid;
        order.Status = OrderStatus.Confirmed;
        await db.SaveChangesAsync();
        Assert.True(await service.ActivatePaidOrderAsync(order.Id));
        var firstEnd = profile.MembershipEndsAtUtc;
        Assert.Equal(72, profile.RemainingClassCredits);
        Assert.Equal(11, profile.ServicePackageVariantId);
        Assert.Equal(2, profile.MembershipPackageId);
        Assert.Equal(order.Id, profile.LastMembershipOrderId);
        Assert.InRange(firstEnd!.Value, DateTime.UtcNow.AddMonths(6).AddMinutes(-1),
            DateTime.UtcNow.AddMonths(6).AddMinutes(1));

        Assert.True(await service.ActivatePaidOrderAsync(order.Id));
        Assert.Equal(firstEnd, profile.MembershipEndsAtUtc);
        Assert.Equal(72, profile.RemainingClassCredits);
    }

    [Fact]
    public async Task CreateOrderAsync_RejectsActiveMembershipAndReusesPendingCheckout()
    {
        await using var db = CreateDb();
        var profile = Seed(db);
        profile.MembershipEndsAtUtc = DateTime.UtcNow.AddDays(2);
        await db.SaveChangesAsync();
        var service = new MembershipRenewalService(db);

        var active = await service.CreateOrderAsync("member-1", 11,
            "Test Member", "05551234567", "Adres", "Kadıköy", "İstanbul");
        Assert.Null(active.OrderId);

        profile.MembershipEndsAtUtc = DateTime.UtcNow.AddDays(-1);
        await db.SaveChangesAsync();
        var first = await service.CreateOrderAsync("member-1", 11,
            "Test Member", "05551234567", "Adres", "Kadıköy", "İstanbul");
        Assert.NotNull(first.OrderId);
        var second = await service.CreateOrderAsync("member-1", 11,
            "Test Member", "05551234567", "Adres", "Kadıköy", "İstanbul");
        Assert.Equal(first.OrderId, second.OrderId);
        Assert.Single(await db.Orders.ToListAsync());
    }

    [Fact]
    public async Task ActivatePaidOrderAsync_DoesNotActivateUnpaidOrder()
    {
        await using var db = CreateDb();
        var profile = Seed(db);
        await db.SaveChangesAsync();
        var service = new MembershipRenewalService(db);
        var result = await service.CreateOrderAsync("member-1", 11,
            "Test Member", "05551234567", "Adres", "Kadıköy", "İstanbul");

        Assert.False(await service.ActivatePaidOrderAsync(result.OrderId!.Value));
        Assert.Equal(3, profile.RemainingClassCredits);
        Assert.Null(profile.LastMembershipOrderId);
    }

    [Fact]
    public async Task IyzicoCallback_ActivatesMembershipOnce_AfterVerifiedPayment()
    {
        await using var db = CreateDb();
        var profile = Seed(db);
        await db.SaveChangesAsync();
        var renewal = new MembershipRenewalService(db);
        var created = await renewal.CreateOrderAsync("member-1", 11,
            "Test Member", "05551234567", "Adres", "Kadıköy", "İstanbul");
        var order = await db.Orders.SingleAsync();
        var payment = new PaymentTransaction
        {
            OrderId = order.Id, Provider = "iyzico", Token = "membership-token",
            ConversationId = "membership-conversation", BasketId = order.OrderNumber,
            PaymentStatus = PaymentStatus.Pending, Amount = order.Total, Currency = "TRY"
        };
        db.PaymentTransactions.Add(payment);
        await db.SaveChangesAsync();
        var fakeClient = new FakeIyzicoCheckoutClient(new IyzicoCheckoutRetrieveResult
        {
            Succeeded = true, PaymentStatus = "SUCCESS", ConversationId = payment.ConversationId,
            BasketId = payment.BasketId, Token = payment.Token
        });
        var checkout = new IyzicoPaymentService(db, fakeClient,
            new KitchenPlanMatchingService(db), Options.Create(new IyzicoOptions()),
            NullLogger<IyzicoPaymentService>.Instance, membershipRenewalService: renewal);

        Assert.True((await checkout.HandleCallbackAsync("membership-token")).Succeeded);
        Assert.Equal(order.Id, profile.LastMembershipOrderId);
        var firstEnd = profile.MembershipEndsAtUtc;
        Assert.True((await checkout.HandleCallbackAsync("membership-token")).Succeeded);
        Assert.Equal(firstEnd, profile.MembershipEndsAtUtc);
        Assert.Equal(72, profile.RemainingClassCredits);
    }

    [Fact]
    public async Task ExpiredCheckoutReconciliation_ActivatesVerifiedMembershipPayment()
    {
        await using var db = CreateDb();
        var profile = Seed(db);
        await db.SaveChangesAsync();
        var renewal = new MembershipRenewalService(db);
        var created = await renewal.CreateOrderAsync("member-1", 11,
            "Test Member", "05551234567", "Adres", "Kadıköy", "İstanbul");
        var order = await db.Orders.SingleAsync();
        var transaction = new PaymentTransaction
        {
            OrderId = created.OrderId!.Value, Provider = "iyzico", Token = "reconcile-token",
            ConversationId = "reconcile-conversation", BasketId = order.OrderNumber,
            PaymentStatus = PaymentStatus.Pending, Amount = order.Total, Currency = "TRY",
            CheckoutExpiresAtUtc = DateTime.UtcNow.AddMinutes(-1)
        };
        db.PaymentTransactions.Add(transaction);
        await db.SaveChangesAsync();
        var fakeClient = new FakeIyzicoCheckoutClient(new IyzicoCheckoutRetrieveResult
        {
            Succeeded = true, PaymentStatus = "SUCCESS", ConversationId = transaction.ConversationId,
            BasketId = transaction.BasketId, Token = transaction.Token
        });
        var reconciliation = new IyzicoPendingPaymentService(db, fakeClient,
            new KitchenPlanMatchingService(db), NullLogger<IyzicoPendingPaymentService>.Instance,
            membershipRenewalService: renewal);

        Assert.Equal(1, await reconciliation.ProcessExpiredPaymentsAsync());
        Assert.Equal(order.Id, profile.LastMembershipOrderId);
        Assert.Equal(72, profile.RemainingClassCredits);
    }

    [Fact]
    public async Task WorkerRecovery_ActivatesPaidMembershipAfterInterruptedCallback()
    {
        await using var db = CreateDb();
        var profile = Seed(db);
        await db.SaveChangesAsync();
        var renewal = new MembershipRenewalService(db);
        var created = await renewal.CreateOrderAsync("member-1", 11,
            "Test Member", "05551234567", "Adres", "Kadıköy", "İstanbul");
        var order = await db.Orders.SingleAsync();
        order.PaymentStatus = PaymentStatus.Paid;
        order.Status = OrderStatus.Confirmed;
        await db.SaveChangesAsync();
        var worker = new IyzicoPendingPaymentService(db,
            new FakeIyzicoCheckoutClient(new IyzicoCheckoutRetrieveResult()),
            new KitchenPlanMatchingService(db), NullLogger<IyzicoPendingPaymentService>.Instance,
            membershipRenewalService: renewal);

        Assert.Equal(1, await worker.ProcessPaidUnactivatedMembershipsAsync());
        Assert.Equal(created.OrderId, profile.LastMembershipOrderId);
        Assert.Equal(0, await worker.ProcessPaidUnactivatedMembershipsAsync());
    }

    [Fact]
    public async Task MembershipCheckout_UsesVirtualItemAndFullTermPrice()
    {
        await using var db = CreateDb();
        Seed(db);
        await db.SaveChangesAsync();
        var created = await new MembershipRenewalService(db).CreateOrderAsync("member-1", 11,
            "Test Member", "05551234567", "Adres", "Kadıköy", "İstanbul");
        var fakeClient = new FakeIyzicoCheckoutClient(new IyzicoCheckoutInitializeResult
        {
            Succeeded = true, Token = "membership-checkout", PaymentPageUrl = "https://example.test/pay",
            TokenExpireTime = 1800
        });
        var options = Options.Create(new IyzicoOptions
        {
            CallbackUrl = "https://example.test/payment/iyzico/callback"
        });
        var checkout = new IyzicoPaymentService(db, fakeClient,
            new KitchenPlanMatchingService(db), options,
            NullLogger<IyzicoPaymentService>.Instance);

        Assert.True((await checkout.InitializeAsync(created.OrderId!.Value, "127.0.0.1")).Succeeded);
        var request = Assert.IsType<IyzicoCheckoutInitializeRequest>(fakeClient.LastInitializeRequest);
        Assert.Equal(72000m, request.PaidPrice);
        Assert.Equal(IyzicoCheckoutItemType.Virtual, Assert.Single(request.Items).ItemType);
    }

    private static ApplicationDbContext CreateDb() => new(
        new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private static MemberProfile Seed(ApplicationDbContext db)
    {
        var legacy = new MembershipPackage { Id = 1, Name = "Legacy" };
        var replacement = new MembershipPackage { Id = 2, Name = "Routine" };
        var package = new ServicePackage
        {
            Id = 10, Name = "Routine", Slug = "routine",
            Category = ServicePackageCategory.Membership,
            MembershipPackageId = replacement.Id, MembershipPackage = replacement
        };
        db.MembershipPackages.Add(legacy);
        db.ServicePackages.Add(package);
        db.ServicePackageVariants.Add(new ServicePackageVariant
        {
            Id = 11, ServicePackageId = package.Id, ServicePackage = package,
            Name = "6 Aylık", DurationMonths = 6, LessonsRenewMonthly = true,
            PersonalTrainingSessionCount = 4, GroupClassCreditCount = 8,
            TotalPrice = 72000m
        });
        var user = new ApplicationUser { Id = "member-1", UserName = "member@test.local",
            Email = "member@test.local" };
        var profile = new MemberProfile
        {
            Id = 20, ApplicationUserId = user.Id, ApplicationUser = user,
            MembershipPackageId = legacy.Id, MembershipPackage = legacy,
            RemainingClassCredits = 3
        };
        db.MemberProfiles.Add(profile);
        return profile;
    }
}
