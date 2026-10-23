using Microsoft.EntityFrameworkCore;
using NO23.Web.Data;
using NO23.Web.Domain.Enums;
using NO23.Web.ViewModels.Admin;

namespace NO23.Web.Services;

public sealed class MemberPaymentHistoryService(ApplicationDbContext db)
{
    public Task<List<MemberPaymentOrderViewModel>> LoadAsync(int[] memberIds) => db.Orders.AsNoTracking()
        .Where(o => o.MemberProfileId.HasValue && memberIds.Contains(o.MemberProfileId.Value))
        .OrderByDescending(o => o.CreatedAtUtc).Select(o => new MemberPaymentOrderViewModel
        {
            Id = o.Id, MemberId = o.MemberProfileId!.Value, OrderNumber = o.OrderNumber,
            Description = o.Items.OrderBy(i => i.Id).Select(i => i.ProductName).FirstOrDefault() ?? "Sipariş",
            Total = o.Total, Subtotal = o.Subtotal, DiscountAmount = o.DiscountAmount,
            Method = o.PaymentMethod, Status = o.Status, PaymentStatus = o.PaymentStatus, CreatedAtUtc = o.CreatedAtUtc,
            OnlinePaidAtUtc = o.PaymentTransactions.Where(p => p.PaymentStatus == PaymentStatus.Paid && p.CompletedAtUtc.HasValue)
                .OrderByDescending(p => p.CompletedAtUtc).Select(p => p.CompletedAtUtc).FirstOrDefault(),
            Installments = o.PaymentInstallments.OrderBy(i => i.Number).Select(i => new MemberReceiptViewModel(
                i.Number, i.Amount, i.DueDate, i.PaidOn, i.PaymentReference)).ToList()
        }).AsSplitQuery().ToListAsync();
}
