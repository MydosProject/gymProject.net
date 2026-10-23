using NO23.Web.Domain.Enums;
using NO23.Web.ViewModels.Admin;

namespace NO23.Tests;

public class MemberPaymentHistoryTests
{
    [Fact]
    public void PartialReceipts_AreCountedOnceAndLatestDayShowsItsActualTotal()
    {
        var day = new DateOnly(2026, 10, 23);
        var first = new MemberPaymentOrderViewModel { Total = 300, PaymentStatus = PaymentStatus.Pending,
            Installments = [new(1,100,day.AddMonths(-2),day.AddDays(-1),null), new(2,100,day,day,null), new(3,100,day.AddMonths(1),null,null)] };
        var second = new MemberPaymentOrderViewModel { Total = 50, PaymentStatus = PaymentStatus.Paid,
            OnlinePaidAtUtc = new DateTime(2026,10,23,10,0,0,DateTimeKind.Utc) };
        var result = MemberPaymentSummary.Calculate([first, second]);
        Assert.Equal(250, result.Collected);
        Assert.Equal(100, result.Outstanding);
        Assert.Equal(day, result.LastReceiptDate);
        Assert.Equal(150, result.LastReceiptDayAmount);
    }

    [Fact]
    public void CancelledAndRefundedOrders_DoNotCreateDebtAndCancellationDoesNotPretendToRefundMoney()
    {
        var cancelled = new MemberPaymentOrderViewModel { Total = 100, Status = OrderStatus.Cancelled, PaymentStatus = PaymentStatus.Pending,
            Installments = [new(1,25,new(2026,10,1),new(2026,10,1),null),new(2,75,new(2026,11,1),null,null)] };
        var refunded = new MemberPaymentOrderViewModel { Total = 50, PaymentStatus = PaymentStatus.Refunded,
            Installments = [new(1,50,new(2026,10,1),new(2026,10,1),null)] };
        var result = MemberPaymentSummary.Calculate([cancelled,refunded]);
        Assert.Equal(25, result.Collected);
        Assert.Equal(0, result.Outstanding);
    }

    [Fact]
    public void LegacyPaidOrder_DoesNotInventAPaymentDateFromItsCreationDate()
    {
        var order = new MemberPaymentOrderViewModel { Total = 80, PaymentStatus = PaymentStatus.Paid, CreatedAtUtc = DateTime.UtcNow };
        var result = MemberPaymentSummary.Calculate([order]);
        Assert.Equal(80, result.Collected);
        Assert.Equal(0, result.Outstanding);
        Assert.Null(result.LastReceiptDate);
    }
}
