using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using NO23.Web.Data;
using NO23.Web.Domain.Entities;
using NO23.Web.Domain.Enums;
using NO23.Web.Services;

namespace NO23.Tests;

public class ManualPaymentRecoveryTests
{
    [Fact]
    public async Task IdempotentReceipt_ReconcilesStalePaidStateWithoutReplacingOriginalAudit()
    {
        var options = CreateOptions();
        await using var db = new ApplicationDbContext(options);
        var order = new Order
        {
            OrderNumber = Guid.NewGuid().ToString("N"), IsManualPayment = true, Total = 100,
            PaymentStatus = PaymentStatus.Pending, Status = OrderStatus.Confirmed,
            PaymentInstallments = [new() { Number = 1, Amount = 40, DueDate = Today }, new() { Number = 2, Amount = 60, DueDate = Today }]
        };
        db.Orders.Add(order);
        await db.SaveChangesAsync();
        var receiptId = order.PaymentInstallments.First().Id;
        await using (var otherDb = new ApplicationDbContext(options))
        {
            var saved = await otherDb.Orders.Include(item => item.PaymentInstallments).SingleAsync();
            foreach (var row in saved.PaymentInstallments)
            {
                row.PaidOn = Today.AddDays(-1);
                row.PaymentReference = $"original-{row.Number}";
                row.RecordedByUserId = "admin-original";
            }
            // Reproduce a stale order status left behind by concurrent final receipts.
            await otherDb.SaveChangesAsync();
        }

        var error = await new ManualOrderPaymentService(db).RecordInstallmentAsync(order.Id, receiptId, Today, "replacement", "admin-new");

        Assert.Null(error);
        Assert.Equal(PaymentStatus.Paid, order.PaymentStatus);
        Assert.All(order.PaymentInstallments, row =>
        {
            Assert.Equal(Today.AddDays(-1), row.PaidOn);
            Assert.Equal($"original-{row.Number}", row.PaymentReference);
            Assert.Equal("admin-original", row.RecordedByUserId);
        });
        await using var verified = new ApplicationDbContext(options);
        Assert.Equal(PaymentStatus.Paid, (await verified.Orders.SingleAsync()).PaymentStatus);
    }

    [Fact]
    public async Task Kitchen_ActivatesOnlyAfterFinalReceiptAndDoesNotDuplicatePlanOnRetry()
    {
        await using var db = new ApplicationDbContext(CreateOptions());
        var order = await SeedKitchenOrderAsync(db);
        AddLunchMenuItem(db);
        await db.SaveChangesAsync();
        var rows = order.PaymentInstallments.OrderBy(item => item.Number).ToArray();
        var logger = new FailureLogger();
        var service = new ManualOrderPaymentService(db, logger);

        Assert.Null(await service.RecordInstallmentAsync(order.Id, rows[0].Id, Today, "first", "admin"));
        Assert.Equal(PaymentStatus.Pending, order.PaymentStatus);
        Assert.Equal(KitchenSubscriptionStatus.PendingPayment, order.KitchenSubscription!.Status);
        Assert.Empty(await db.KitchenMealPlans.ToListAsync());
        Assert.NotNull(await service.RetryActivationAsync(order.Id));

        Assert.Null(await service.RecordInstallmentAsync(order.Id, rows[1].Id, Today, "last", "admin"));
        Assert.Equal(PaymentStatus.Paid, order.PaymentStatus);
        Assert.Equal(KitchenSubscriptionStatus.Active, order.KitchenSubscription.Status);
        Assert.Equal(Today.AddDays(1), order.KitchenSubscription.StartsOn);
        Assert.Equal(Today.AddDays(5), order.KitchenSubscription.EndsOn);
        var plan = await db.KitchenMealPlans.Include(item => item.Days).SingleAsync();
        Assert.Equal(5, plan.Days.Count);

        var retryError = await service.RetryActivationAsync(order.Id);
        Assert.True(retryError is null, logger.LastException?.ToString() ?? retryError);
        Assert.Single(await db.KitchenMealPlans.ToListAsync());
        Assert.Equal("first", rows[0].PaymentReference);
        Assert.Equal("last", rows[1].PaymentReference);
    }

    [Fact]
    public async Task KitchenPlanFailure_PreservesPaymentAndCanBeRetriedAfterMenuCorrection()
    {
        await using var db = new ApplicationDbContext(CreateOptions());
        var order = await SeedKitchenOrderAsync(db);
        var rows = order.PaymentInstallments.OrderBy(item => item.Number).ToArray();
        var service = new ManualOrderPaymentService(db);
        Assert.Null(await service.RecordInstallmentAsync(order.Id, rows[0].Id, Today, "first", "admin"));

        var error = await service.RecordInstallmentAsync(order.Id, rows[1].Id, Today, "last", "admin");

        Assert.NotNull(error);
        Assert.Equal(PaymentStatus.Paid, order.PaymentStatus);
        Assert.All(order.PaymentInstallments, row => Assert.NotNull(row.PaidOn));
        Assert.Equal(KitchenSubscriptionStatus.Paused, order.KitchenSubscription!.Status);
        Assert.Empty(await db.KitchenMealPlans.ToListAsync());
        AddLunchMenuItem(db);
        await db.SaveChangesAsync();

        Assert.Null(await service.RetryActivationAsync(order.Id));
        Assert.Equal(KitchenSubscriptionStatus.Active, order.KitchenSubscription.Status);
        Assert.Single(await db.KitchenMealPlans.ToListAsync());
        Assert.Equal("last", rows[1].PaymentReference);
    }

    [Fact]
    public async Task KitchenPlanException_PreservesCommittedReceiptAndFreshRequestCanRetry()
    {
        var interceptor = new ThrowOnceWhenSavingKitchenPlan();
        var options = CreateOptions(interceptor);
        int orderId;
        await using (var db = new ApplicationDbContext(options))
        {
            var order = await SeedKitchenOrderAsync(db);
            orderId = order.Id;
            AddLunchMenuItem(db);
            await db.SaveChangesAsync();
            var rows = order.PaymentInstallments.OrderBy(item => item.Number).ToArray();
            var service = new ManualOrderPaymentService(db);
            Assert.Null(await service.RecordInstallmentAsync(order.Id, rows[0].Id, Today, "first", "admin"));

            var error = await service.RecordInstallmentAsync(order.Id, rows[1].Id, Today, "last", "admin");

            Assert.Contains("yeniden deneyin", error);
            Assert.True(interceptor.HasThrown);
        }

        await using var retryDb = new ApplicationDbContext(options);
        var saved = await retryDb.Orders.Include(item => item.PaymentInstallments).SingleAsync();
        Assert.Equal(PaymentStatus.Paid, saved.PaymentStatus);
        Assert.All(saved.PaymentInstallments, row => Assert.NotNull(row.PaidOn));
        Assert.Equal("last", saved.PaymentInstallments.Single(item => item.Number == 2).PaymentReference);
        Assert.Empty(await retryDb.KitchenMealPlans.ToListAsync());

        Assert.Null(await new ManualOrderPaymentService(retryDb).RetryActivationAsync(orderId));
        Assert.Equal(KitchenSubscriptionStatus.Active, (await retryDb.KitchenSubscriptions.SingleAsync()).Status);
        Assert.Single(await retryDb.KitchenMealPlans.ToListAsync());
    }

    [Fact]
    public async Task RetryActivation_ReplacesExistingFailedDraftAndPreservesReceipts()
    {
        await using var db = new ApplicationDbContext(CreateOptions());
        var order = await SeedKitchenOrderAsync(db);
        foreach (var row in order.PaymentInstallments)
        {
            row.PaidOn = Today.AddDays(-1);
            row.PaymentReference = $"receipt-{row.Number}";
            row.RecordedByUserId = "admin-original";
        }
        order.PaymentStatus = PaymentStatus.Paid;
        order.KitchenSubscription!.Status = KitchenSubscriptionStatus.Paused;
        var failedPlan = new KitchenMealPlan
        {
            KitchenSubscription = order.KitchenSubscription, Status = KitchenMealPlanStatus.Failed,
            Days = [new() { DayNumber = 1, PlanDate = Today.AddDays(-10) }]
        };
        db.KitchenMealPlans.Add(failedPlan);
        AddLunchMenuItem(db);
        await db.SaveChangesAsync();
        var failedPlanId = failedPlan.Id;

        var logger = new FailureLogger();
        var retryError = await new ManualOrderPaymentService(db, logger).RetryActivationAsync(order.Id);
        Assert.True(retryError is null, logger.LastException?.ToString() ?? retryError);

        var regenerated = await db.KitchenMealPlans.Include(item => item.Days).SingleAsync();
        Assert.NotEqual(failedPlanId, regenerated.Id);
        Assert.Equal(KitchenMealPlanStatus.Generated, regenerated.Status);
        Assert.Equal(5, regenerated.Days.Count);
        Assert.Equal(KitchenSubscriptionStatus.Active, order.KitchenSubscription.Status);
        Assert.All(order.PaymentInstallments, row =>
        {
            Assert.Equal(Today.AddDays(-1), row.PaidOn);
            Assert.Equal($"receipt-{row.Number}", row.PaymentReference);
            Assert.Equal("admin-original", row.RecordedByUserId);
        });
    }

    [Theory]
    [InlineData("unpaid")]
    [InlineData("cancelled")]
    [InlineData("refunded")]
    [InlineData("non-manual")]
    [InlineData("not-kitchen")]
    public async Task RetryActivation_RejectsIneligibleOrderWithoutCreatingPlan(string problem)
    {
        await using var db = new ApplicationDbContext(CreateOptions());
        var order = await SeedKitchenOrderAsync(db);
        AddLunchMenuItem(db);
        if (problem != "unpaid")
        {
            foreach (var row in order.PaymentInstallments) row.PaidOn = Today;
            order.PaymentStatus = PaymentStatus.Paid;
        }
        if (problem == "cancelled") order.Status = OrderStatus.Cancelled;
        if (problem == "refunded") order.PaymentStatus = PaymentStatus.Refunded;
        if (problem == "non-manual") order.IsManualPayment = false;
        if (problem == "not-kitchen") order.Type = OrderType.OneTime;
        await db.SaveChangesAsync();
        var initialStatus = order.PaymentStatus;

        Assert.NotNull(await new ManualOrderPaymentService(db).RetryActivationAsync(order.Id));

        Assert.Equal(initialStatus, order.PaymentStatus);
        Assert.Equal(KitchenSubscriptionStatus.PendingPayment, order.KitchenSubscription!.Status);
        Assert.Empty(await db.KitchenMealPlans.ToListAsync());
    }

    private static DateOnly Today => DateOnly.FromDateTime(ClubTime.Now);

    private static DbContextOptions<ApplicationDbContext> CreateOptions(IInterceptor? interceptor = null)
    {
        var builder = new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString());
        if (interceptor is not null) builder.AddInterceptors(interceptor);
        return builder.Options;
    }

    private static void AddLunchMenuItem(ApplicationDbContext db) => db.KitchenMenuItems.Add(new()
    {
        Name = "Öğle öğünü", Ingredients = "Test içerik", Category = MenuItemCategory.MainMeal,
        Calories = 600, UnitPrice = 100, ProteinGrams = 30, CarbohydrateGrams = 60, FatGrams = 20
    });

    private static async Task<Order> SeedKitchenOrderAsync(ApplicationDbContext db)
    {
        var member = new MemberProfile
        {
            ApplicationUser = new ApplicationUser { UserName = "test@example.com", Email = "test@example.com" },
            MembershipPackage = new MembershipPackage { Code = MembershipPackageCode.Pro, Name = "Pro", Audience = "Test", Description = "Test" }
        };
        var order = new Order
        {
            OrderNumber = Guid.NewGuid().ToString("N"), Type = OrderType.KitchenSubscription,
            PaymentMethod = PaymentMethod.Cash, IsManualPayment = true, Total = 100,
            PaymentInstallments = [new() { Number = 1, Amount = 40, DueDate = Today }, new() { Number = 2, Amount = 60, DueDate = Today }],
            MemberProfile = member,
            KitchenSubscription = new KitchenSubscription
            {
                MemberProfile = member, Plan = KitchenSubscriptionPlan.FiveDays, SelectedMealSlotsMask = 4,
                PackageNameSnapshot = "5 gün", PackageDaysSnapshot = 5, PackagePriceSnapshot = 100,
                StartsOn = Today.AddDays(-10), EndsOn = Today.AddDays(-6),
                KitchenSubscriptionPackage = new KitchenSubscriptionPackage { Name = "5 gün", Days = 5, Plan = KitchenSubscriptionPlan.FiveDays }
            }
        };
        db.Orders.Add(order);
        await db.SaveChangesAsync();
        return order;
    }

    private sealed class ThrowOnceWhenSavingKitchenPlan : SaveChangesInterceptor
    {
        public bool HasThrown { get; private set; }

        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData,
            InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            if (!HasThrown && eventData.Context!.ChangeTracker.Entries<KitchenMealPlan>().Any(entry => entry.State == EntityState.Added))
            {
                HasThrown = true;
                throw new InvalidOperationException("Simulated plan write failure");
            }
            return base.SavingChangesAsync(eventData, result, cancellationToken);
        }
    }

    private sealed class FailureLogger : ILogger<ManualOrderPaymentService>
    {
        public Exception? LastException { get; private set; }
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter) => LastException = exception;
    }
}
