using System.Globalization;
using Microsoft.EntityFrameworkCore;
using NO23.Web.Data;
using NO23.Web.Domain.Entities;
using NO23.Web.Domain.Enums;
using NO23.Web.Services;
using NO23.Web.ViewModels.Admin;

namespace NO23.Tests;

public class ManualOrderPaymentServiceTests
{
    [Theory]
    [InlineData(PaymentMethod.Cash)]
    [InlineData(PaymentMethod.BankTransfer)]
    public void AdminMembershipOrder_AcceptsCashAndBankTransfer(PaymentMethod method)
    {
        var input = PaymentInput();
        input.Method = method;

        var result = ManualOrderPaymentService.PreparePackageOrder(Variant(ServicePackageCategory.Membership), input);

        Assert.Empty(result.Errors);
        Assert.Equal(method, result.Order!.PaymentMethod);
        Assert.True(result.Order.IsManualPayment);
    }

    [Theory]
    [InlineData(ServicePackageCategory.Membership, PaymentMethod.CreditCard)]
    [InlineData(ServicePackageCategory.PersonalTraining, PaymentMethod.CreditCard)]
    [InlineData(ServicePackageCategory.PersonalTraining, PaymentMethod.Cash)]
    [InlineData(ServicePackageCategory.PersonalTraining, PaymentMethod.BankTransfer)]
    [InlineData(ServicePackageCategory.GroupClasses, PaymentMethod.CreditCard)]
    [InlineData(ServicePackageCategory.GroupClasses, PaymentMethod.Cash)]
    [InlineData(ServicePackageCategory.GroupClasses, PaymentMethod.BankTransfer)]
    [InlineData(ServicePackageCategory.KidsClub, PaymentMethod.CreditCard)]
    [InlineData(ServicePackageCategory.KidsClub, PaymentMethod.Cash)]
    [InlineData(ServicePackageCategory.KidsClub, PaymentMethod.BankTransfer)]
    public void PreparePackageOrder_AcceptsSupportedMethodsAndCreatesPackagePayment(ServicePackageCategory category, PaymentMethod method)
    {
        var variant = Variant(category);
        var input = PaymentInput();
        input.Method = method;
        input.Reference = "  POS-123  ";

        var result = ManualOrderPaymentService.PreparePackageOrder(variant, input, recordedByUserId: "admin-1");

        Assert.Empty(result.Errors);
        var order = Assert.IsType<Order>(result.Order);
        Assert.True(order.IsManualPayment);
        Assert.Equal(OrderType.ServicePackagePurchase, order.Type);
        Assert.Equal(OrderStatus.Confirmed, order.Status);
        Assert.Equal(PaymentStatus.Pending, order.PaymentStatus);
        Assert.Equal(method, order.PaymentMethod);
        Assert.Equal("POS-123", order.PaymentReference);
        Assert.Equal("admin-1", order.PaymentRecordedByUserId);
        Assert.Equal(100m, order.Total);
        Assert.Equal(variant.Id, order.ServicePackageVariantId);
        var item = Assert.Single(order.Items);
        Assert.Equal("TEST — 1 Ay", item.ProductName);
        Assert.Equal(1, item.Quantity);
        Assert.Equal(order.Total, item.LineTotal);
        Assert.Equal(order.Total, Assert.Single(order.PaymentInstallments).Amount);
    }

    [Fact]
    public void IsMethodAllowed_EnforcesMembershipRuleForExistingRenewalAndPackageOrders()
    {
        var renewal = new Order { Type = OrderType.MembershipRenewal };
        var membership = new Order { Type = OrderType.ServicePackagePurchase, ServicePackageVariant = Variant(ServicePackageCategory.Membership) };
        var pt = new Order { Type = OrderType.ServicePackagePurchase, ServicePackageVariant = Variant(ServicePackageCategory.PersonalTraining) };
        foreach (var method in Enum.GetValues<PaymentMethod>())
        {
            Assert.Equal(method == PaymentMethod.CreditCard, ManualOrderPaymentService.IsMethodAllowed(renewal, method));
            Assert.Equal(method == PaymentMethod.CreditCard, ManualOrderPaymentService.IsMethodAllowed(membership, method));
            Assert.True(ManualOrderPaymentService.IsMethodAllowed(pt, method));
        }
        Assert.False(ManualOrderPaymentService.IsMethodAllowed(pt, (PaymentMethod)999));
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-1")]
    [InlineData("1.001")]
    [InlineData("100000000")]
    public void PreparePackageOrder_RejectsNonPositiveOversizedOrOverpreciseAmounts(string amount)
    {
        var variant = Variant(ServicePackageCategory.PersonalTraining);
        variant.PriceOnRequest = true;
        var input = PaymentInput();
        input.Amount = decimal.Parse(amount, CultureInfo.InvariantCulture);

        var result = ManualOrderPaymentService.PreparePackageOrder(variant, input);

        Assert.Null(result.Order);
        Assert.Contains(result.Errors, error => error.MemberNames.Contains("Payment.Amount"));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void AdminPackageOrder_AllowsAnAgreedAmountForEveryPackage(bool priceOnRequest)
    {
        var variant = Variant(ServicePackageCategory.PersonalTraining);
        variant.PriceOnRequest = priceOnRequest;
        var input = PaymentInput();
        input.Amount = 125m;

        var result = ManualOrderPaymentService.PreparePackageOrder(variant, input);

        Assert.Empty(result.Errors);
        Assert.Equal(125m, result.Order!.Total);
        Assert.Equal(result.Order.Subtotal - result.Order.DiscountAmount, result.Order.Total);
        Assert.Contains("Katalog bedeli:", result.Order.Notes);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void AdminDiscountOrDirectAmount_PreservesPriceAndInstallmentTotals(bool directAmount)
    {
        var variant = Variant(ServicePackageCategory.Membership);
        var input = PaymentInput();
        input.Method = PaymentMethod.Cash;
        input.DiscountPercent = directAmount ? 0 : 20;
        input.Amount = directAmount ? 80 : null;
        input.PriceAdjustmentNote = "Üyeye özel anlaşma";
        input.InstallmentCount = 3;
        var result = ManualOrderPaymentService.PreparePackageOrder(variant, input, recordedByUserId: "admin-qa");
        Assert.Empty(result.Errors);
        var order = result.Order!;
        Assert.Equal(100m, order.Subtotal);
        Assert.Equal(20m, order.DiscountAmount);
        Assert.Equal(80m, order.Total);
        Assert.Equal(order.Total, order.PaymentInstallments.Sum(i => i.Amount));
        Assert.Equal(order.Subtotal, order.Items.Sum(i => i.LineTotal));
        Assert.Equal("admin-qa", order.PaymentRecordedByUserId);
        Assert.Contains(input.PriceAdjustmentNote, order.Notes);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(101)]
    public void AdminDiscount_RejectsInvalidPercentEvenWhenAnAmountIsPosted(int percent)
    {
        var input = PaymentInput(); input.DiscountPercent = percent; input.Amount = 50;
        var result = ManualOrderPaymentService.PreparePackageOrder(Variant(ServicePackageCategory.Membership), input);
        Assert.Null(result.Order);
        Assert.Contains(result.Errors, e => e.MemberNames.Contains("Payment.DiscountPercent"));
    }

    [Fact]
    public void PreparePackageOrder_UsesPaidMonthlyTermAndAppliesDiscountBeforeInstallmentSplit()
    {
        var variant = Variant(ServicePackageCategory.Membership);
        variant.BillingType = ServicePackageBillingType.MonthlySubscription;
        variant.MonthlyPrice = 500m;
        variant.DurationMonths = 2;
        variant.BonusMonths = 1;
        var input = PaymentInput();
        input.InstallmentCount = 3;

        var result = ManualOrderPaymentService.PreparePackageOrder(variant, input, discountPercent: 25);

        Assert.Empty(result.Errors);
        Assert.Equal(750m, result.Order!.Total);
        Assert.Equal(new[] { 250m, 250m, 250m }, result.Order.PaymentInstallments.Select(row => row.Amount));
    }

    [Fact]
    public void BuildInstallments_PreservesExactTotalAndMonthEndDatesWithFinalRoundingCorrection()
    {
        var rows = ManualOrderPaymentService.BuildInstallments(100m, 3, new DateOnly(2026, 1, 31));

        Assert.Equal(new decimal?[] { 33.33m, 33.33m, 33.34m }, rows.Select(row => row.Amount));
        Assert.Equal(100m, rows.Sum(row => row.Amount));
        Assert.Equal(new[] { new DateOnly(2026, 1, 31), new DateOnly(2026, 2, 28), new DateOnly(2026, 3, 31) }, rows.Select(row => row.DueDate));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(37)]
    public void PreparePackageOrder_RejectsInstallmentCountsOutsideSupportedRange(int count)
    {
        var input = PaymentInput();
        input.InstallmentCount = count;

        var result = ManualOrderPaymentService.PreparePackageOrder(Variant(ServicePackageCategory.PersonalTraining), input);

        Assert.Null(result.Order);
        Assert.Contains(result.Errors, error => error.MemberNames.Contains("Payment.InstallmentCount"));
    }

    [Theory]
    [InlineData("count", "Payment.InstallmentCount")]
    [InlineData("sum", "Payment.Installments")]
    [InlineData("zero-row", "Payment.Installments[0].Amount")]
    [InlineData("overprecise-row", "Payment.Installments[0].Amount")]
    [InlineData("due-order", "Payment.Installments[1].DueDate")]
    [InlineData("due-range", "Payment.Installments[0].DueDate")]
    [InlineData("paid-future", "Payment.Installments[0].PaidOn")]
    [InlineData("paid-old", "Payment.Installments[0].PaidOn")]
    public void PreparePackageOrder_RejectsInconsistentAmountsAndDatesInExplicitSchedule(string problem, string errorKey)
    {
        var input = PaymentInput();
        input.InstallmentCount = 2;
        input.Installments = [new() { Amount = 40m, DueDate = Today }, new() { Amount = 60m, DueDate = Today.AddMonths(1) }];
        switch (problem)
        {
            case "count": input.Installments.RemoveAt(1); break;
            case "sum": input.Installments[1].Amount = 59.99m; break;
            case "zero-row": input.Installments[0].Amount = 0; input.Installments[1].Amount = 100m; break;
            case "overprecise-row": input.Installments[0].Amount = 40.001m; input.Installments[1].Amount = 59.999m; break;
            case "due-order": input.Installments[1].DueDate = Today.AddDays(-1); break;
            case "due-range": input.Installments[0].DueDate = Today.AddYears(6); break;
            case "paid-future": input.Installments[0].PaidOn = Today.AddDays(1); break;
            case "paid-old": input.Installments[0].PaidOn = Today.AddYears(-10).AddDays(-1); break;
        }

        var result = ManualOrderPaymentService.PreparePackageOrder(Variant(ServicePackageCategory.PersonalTraining), input);

        Assert.Null(result.Order);
        Assert.Contains(result.Errors, error => error.MemberNames.Contains(errorKey));
    }

    [Theory]
    [InlineData("first-due-past", "Payment.FirstDueDate")]
    [InlineData("first-due-future", "Payment.FirstDueDate")]
    [InlineData("reference", "Payment.Reference")]
    [InlineData("method", "Payment.Method")]
    public void PreparePackageOrder_RejectsInvalidPaymentHeader(string problem, string errorKey)
    {
        var input = PaymentInput();
        if (problem == "first-due-past") input.FirstDueDate = Today.AddYears(-10).AddDays(-1);
        if (problem == "first-due-future") input.FirstDueDate = Today.AddYears(5).AddDays(1);
        if (problem == "reference") input.Reference = new string('x', 161);
        if (problem == "method") input.Method = (PaymentMethod)999;

        var result = ManualOrderPaymentService.PreparePackageOrder(Variant(ServicePackageCategory.PersonalTraining), input);

        Assert.Null(result.Order);
        Assert.Contains(result.Errors, error => error.MemberNames.Contains(errorKey));
    }

    [Theory]
    [InlineData(0, PaymentStatus.Pending)]
    [InlineData(1, PaymentStatus.Pending)]
    [InlineData(2, PaymentStatus.Paid)]
    public void PreparePackageOrder_OnlyMarksFullyCollectedScheduleAsPaid(int paidRows, PaymentStatus status)
    {
        var input = PaymentInput();
        input.InstallmentCount = 2;
        input.Reference = "  RECEIPT-1  ";
        input.Installments = Enumerable.Range(0, 2).Select(index => new PaymentInstallmentInputViewModel
        {
            Amount = 50m, DueDate = Today.AddMonths(index), PaidOn = index < paidRows ? Today : null
        }).ToList();

        var result = ManualOrderPaymentService.PreparePackageOrder(Variant(ServicePackageCategory.PersonalTraining), input, recordedByUserId: "admin-1");

        Assert.Empty(result.Errors);
        Assert.Equal(status, result.Order!.PaymentStatus);
        foreach (var row in result.Order.PaymentInstallments)
        {
            Assert.Equal(row.Number <= paidRows ? "RECEIPT-1" : null, row.PaymentReference);
            Assert.Equal(row.Number <= paidRows ? "admin-1" : null, row.RecordedByUserId);
        }
    }

    [Fact]
    public async Task RecordInstallment_UpdatesOnlyBelongingInstallmentAndCompletesOrderAfterLastCollection()
    {
        await using var dbContext = CreateDbContext();
        var order = await SeedManualOrderAsync(dbContext);
        var otherOrder = await SeedManualOrderAsync(dbContext);
        var service = new ManualOrderPaymentService(dbContext);
        var rows = order.PaymentInstallments.OrderBy(row => row.Number).ToArray();

        Assert.NotNull(await service.RecordInstallmentAsync(order.Id, otherOrder.PaymentInstallments.First().Id, Today, "wrong", "admin-1"));
        Assert.All(order.PaymentInstallments, row => Assert.Null(row.PaidOn));
        Assert.Null(await service.RecordInstallmentAsync(order.Id, rows[0].Id, Today, "  RECEIPT-1  ", "admin-1"));
        Assert.Equal(PaymentStatus.Pending, order.PaymentStatus);
        Assert.Equal(Today, rows[0].PaidOn);
        Assert.Equal("RECEIPT-1", rows[0].PaymentReference);
        Assert.Equal("admin-1", rows[0].RecordedByUserId);
        Assert.Null(rows[1].PaidOn);
        Assert.Null(await service.RecordInstallmentAsync(order.Id, rows[1].Id, Today, "RECEIPT-2", "admin-2"));
        Assert.Equal(PaymentStatus.Paid, order.PaymentStatus);
        Assert.All(otherOrder.PaymentInstallments, row => Assert.Null(row.PaidOn));
        dbContext.ChangeTracker.Clear();
        var savedOrder = await dbContext.Orders.Include(item => item.PaymentInstallments).SingleAsync(item => item.Id == order.Id);
        Assert.Equal(PaymentStatus.Paid, savedOrder.PaymentStatus);
        Assert.All(savedOrder.PaymentInstallments, row => Assert.NotNull(row.PaidOn));
    }

    [Fact]
    public async Task RecordInstallment_IsIdempotentAndPreservesOriginalReceiptAudit()
    {
        await using var dbContext = CreateDbContext();
        var order = await SeedManualOrderAsync(dbContext);
        var row = order.PaymentInstallments.First();
        var service = new ManualOrderPaymentService(dbContext);
        var paidOn = Today.AddDays(-1);
        Assert.Null(await service.RecordInstallmentAsync(order.Id, row.Id, paidOn, "original", "admin-1"));
        var updatedAt = order.UpdatedAtUtc;

        Assert.Null(await service.RecordInstallmentAsync(order.Id, row.Id, Today, "replacement", "admin-2"));

        Assert.Equal(paidOn, row.PaidOn);
        Assert.Equal("original", row.PaymentReference);
        Assert.Equal("admin-1", row.RecordedByUserId);
        Assert.Equal(updatedAt, order.UpdatedAtUtc);
        Assert.Equal(PaymentStatus.Pending, order.PaymentStatus);
    }

    [Theory]
    [InlineData("future")]
    [InlineData("too-old")]
    [InlineData("cancelled")]
    [InlineData("refunded")]
    [InlineData("non-manual")]
    [InlineData("long-reference")]
    public async Task RecordInstallment_RejectsInvalidCollectionWithoutChangingOrderOrSchedule(string problem)
    {
        await using var dbContext = CreateDbContext();
        var order = await SeedManualOrderAsync(dbContext);
        if (problem == "cancelled") order.Status = OrderStatus.Cancelled;
        if (problem == "refunded") order.PaymentStatus = PaymentStatus.Refunded;
        if (problem == "non-manual") order.IsManualPayment = false;
        order.UpdatedAtUtc = DateTime.UnixEpoch;
        await dbContext.SaveChangesAsync();
        var initialPaymentStatus = order.PaymentStatus;
        var paidOn = problem switch { "future" => Today.AddDays(1), "too-old" => Today.AddYears(-10).AddDays(-1), _ => Today };
        var reference = problem == "long-reference" ? new string('x', 161) : "receipt";

        var error = await new ManualOrderPaymentService(dbContext).RecordInstallmentAsync(order.Id, order.PaymentInstallments.First().Id, paidOn, reference, "admin-1");

        Assert.NotNull(error);
        Assert.Equal(initialPaymentStatus, order.PaymentStatus);
        Assert.Equal(DateTime.UnixEpoch, order.UpdatedAtUtc);
        Assert.All(order.PaymentInstallments, row =>
        {
            Assert.Null(row.PaidOn);
            Assert.Null(row.PaymentReference);
            Assert.Null(row.RecordedByUserId);
        });
    }

    private static DateOnly Today => DateOnly.FromDateTime(ClubTime.Now);

    private static ManualPaymentInputViewModel PaymentInput() => new() { Method = PaymentMethod.CreditCard, FirstDueDate = Today };

    private static ServicePackageVariant Variant(ServicePackageCategory category) => new()
    {
        Id = 100,
        ServicePackage = new ServicePackage { Name = "TEST", Slug = "test", Category = category },
        Name = "1 Ay",
        BillingType = ServicePackageBillingType.OneTime,
        TotalPrice = 100m,
        DurationMonths = 1
    };

    private static ApplicationDbContext CreateDbContext() => new(new DbContextOptionsBuilder<ApplicationDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private static async Task<Order> SeedManualOrderAsync(ApplicationDbContext dbContext)
    {
        var order = new Order
        {
            OrderNumber = Guid.NewGuid().ToString("N"),
            IsManualPayment = true,
            Status = OrderStatus.Confirmed,
            PaymentStatus = PaymentStatus.Pending,
            Total = 100m,
            PaymentInstallments = [new() { Number = 1, Amount = 50m, DueDate = Today }, new() { Number = 2, Amount = 50m, DueDate = Today.AddMonths(1) }]
        };
        dbContext.Orders.Add(order);
        await dbContext.SaveChangesAsync();
        return order;
    }
}
