using NO23.Web.Domain.Enums;
using NO23.Web.Services;

namespace NO23.Web.ViewModels.Admin;

public class MemberPaymentHistoryViewModel
{
    public int MemberId { get; init; }
    public string MemberName { get; init; } = "";
    public IReadOnlyList<MemberPaymentOrderViewModel> Orders { get; init; } = [];
    public MemberPaymentSummary Summary => MemberPaymentSummary.Calculate(Orders);
}

public class MemberPaymentOrderViewModel
{
    public int Id { get; init; }
    public int MemberId { get; init; }
    public string OrderNumber { get; init; } = "";
    public string Description { get; init; } = "";
    public decimal Total { get; init; }
    public decimal Subtotal { get; init; }
    public decimal DiscountAmount { get; init; }
    public PaymentMethod Method { get; init; }
    public PaymentStatus PaymentStatus { get; init; }
    public OrderStatus Status { get; init; }
    public DateTime CreatedAtUtc { get; init; }
    public DateTime? OnlinePaidAtUtc { get; init; }
    public IReadOnlyList<MemberReceiptViewModel> Installments { get; init; } = [];
    public decimal Collected => PaymentStatus == PaymentStatus.Refunded ? 0 : Installments.Count > 0
        ? Installments.Where(i => i.PaidOn.HasValue).Sum(i => i.Amount) : PaymentStatus == PaymentStatus.Paid ? Total : 0;
    public decimal Outstanding => Status == OrderStatus.Cancelled || PaymentStatus is PaymentStatus.Refunded or PaymentStatus.Failed or PaymentStatus.Expired
        ? 0 : Math.Max(0, Total - Collected);
    public IEnumerable<(DateOnly Date, decimal Amount)> Receipts => PaymentStatus == PaymentStatus.Refunded ? [] : Installments.Count > 0
        ? Installments.Where(i => i.PaidOn.HasValue).Select(i => (i.PaidOn!.Value, i.Amount))
        : PaymentStatus == PaymentStatus.Paid && OnlinePaidAtUtc.HasValue
            ? [(DateOnly.FromDateTime(ClubTime.ToLocal(OnlinePaidAtUtc.Value)), Total)] : [];
}

public record MemberReceiptViewModel(int Number, decimal Amount, DateOnly DueDate, DateOnly? PaidOn, string? Reference);

public class MemberPaymentSummary
{
    public decimal Collected { get; init; }
    public decimal Outstanding { get; init; }
    public DateOnly? LastReceiptDate { get; init; }
    public decimal LastReceiptDayAmount { get; init; }
    public static MemberPaymentSummary Calculate(IEnumerable<MemberPaymentOrderViewModel> orders)
    {
        var rows = orders.ToList();
        var latestDay = rows.SelectMany(o => o.Receipts).GroupBy(r => r.Date).OrderByDescending(g => g.Key).FirstOrDefault();
        return new() { Collected = rows.Sum(o => o.Collected), Outstanding = rows.Sum(o => o.Outstanding),
            LastReceiptDate = latestDay?.Key, LastReceiptDayAmount = latestDay?.Sum(r => r.Amount) ?? 0 };
    }
}
