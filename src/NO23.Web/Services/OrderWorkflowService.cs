using Microsoft.EntityFrameworkCore;
using NO23.Web.Data;
using NO23.Web.Domain.Entities;
using NO23.Web.Domain.Enums;

namespace NO23.Web.Services;

public class OrderWorkflowService(ApplicationDbContext dbContext)
{
    public static IReadOnlyList<OrderStatus> GetAvailableOrderStatuses(
        OrderStatus currentStatus,
        PaymentStatus paymentStatus)
    {
        if (IsTerminal(currentStatus))
        {
            return [];
        }

        return currentStatus switch
        {
            OrderStatus.Pending when paymentStatus == PaymentStatus.Paid =>
                [OrderStatus.Confirmed],
            OrderStatus.Pending when paymentStatus is
            PaymentStatus.Pending or
            PaymentStatus.Failed or
            PaymentStatus.Expired =>
                [OrderStatus.Cancelled],
            OrderStatus.Confirmed => [OrderStatus.Preparing],
            OrderStatus.Preparing => [OrderStatus.OutForDelivery],
            OrderStatus.OutForDelivery => [OrderStatus.Delivered],
            _ => []
        };
    }

    public static IReadOnlyList<PaymentStatus> GetAvailablePaymentStatuses(
    OrderStatus orderStatus,
    PaymentStatus currentPaymentStatus)
    {
        if (IsTerminal(orderStatus) ||
            currentPaymentStatus == PaymentStatus.Refunded)
        {
            return [];
        }

        return currentPaymentStatus switch
        {
            PaymentStatus.Pending =>
                [PaymentStatus.Paid, PaymentStatus.Failed],

            PaymentStatus.Paid when orderStatus != OrderStatus.Delivered =>
                [PaymentStatus.Refunded],

            _ => []
        };
    }

    public async Task<OrderWorkflowResult> UpdateOrderStatusAsync(
        int orderId,
        OrderStatus requestedStatus)
    {
        var order = await LoadOrderForUpdateAsync(orderId);

        if (order is null)
        {
            return OrderWorkflowResult.Fail("Siparis bulunamadi.");
        }

        if (order.Status == requestedStatus)
        {
            return OrderWorkflowResult.Ok(order.Id);
        }

        var availableStatuses = GetAvailableOrderStatuses(order.Status, order.PaymentStatus);

        if (!availableStatuses.Contains(requestedStatus))
        {
            return OrderWorkflowResult.Fail(GetInvalidOrderStatusMessage(order, requestedStatus));
        }

        order.Status = requestedStatus;
        order.UpdatedAtUtc = DateTime.UtcNow;

        if (requestedStatus == OrderStatus.Cancelled)
        {
            RestoreShopProductStockOnce(order);
        }

        await dbContext.SaveChangesAsync();

        return OrderWorkflowResult.Ok(order.Id);
    }

    public async Task<OrderWorkflowResult> UpdatePaymentStatusAsync(
        int orderId,
        PaymentStatus requestedPaymentStatus)
    {
        var order = await LoadOrderForUpdateAsync(orderId);

        if (order is null)
        {
            return OrderWorkflowResult.Fail("Siparis bulunamadi.");
        }

        if (order.PaymentStatus == requestedPaymentStatus)
        {
            return OrderWorkflowResult.Ok(order.Id);
        }

        var availablePaymentStatuses = GetAvailablePaymentStatuses(
            order.Status,
            order.PaymentStatus);

        if (!availablePaymentStatuses.Contains(requestedPaymentStatus))
        {
            return OrderWorkflowResult.Fail(GetInvalidPaymentStatusMessage(order, requestedPaymentStatus));
        }

        order.PaymentStatus = requestedPaymentStatus;
        order.UpdatedAtUtc = DateTime.UtcNow;

        if (requestedPaymentStatus is PaymentStatus.Failed or PaymentStatus.Refunded)
        {
            order.Status = OrderStatus.Cancelled;
            RestoreShopProductStockOnce(order);
        }

        await dbContext.SaveChangesAsync();

        return OrderWorkflowResult.Ok(order.Id);
    }

    private async Task<Order?> LoadOrderForUpdateAsync(int orderId)
    {
        return await dbContext.Orders
            .Include(order => order.Items)
            .ThenInclude(item => item.ShopProduct)
            .FirstOrDefaultAsync(order => order.Id == orderId);
    }

    public static void RestoreShopProductStockOnce(Order order)
{
    if (order.StockRestoredAtUtc.HasValue)
    {
        return;
    }

    var restoredAtUtc = DateTime.UtcNow;

    foreach (var item in order.Items.Where(item =>
        item.ItemType == CartItemType.ShopProduct &&
        item.ShopProduct is not null))
    {
        item.ShopProduct!.StockQuantity += item.Quantity;
        item.ShopProduct.UpdatedAtUtc = restoredAtUtc;
    }

    order.StockRestoredAtUtc = restoredAtUtc;
    order.UpdatedAtUtc = restoredAtUtc;
    }

    private static bool IsTerminal(OrderStatus status)
    {
        return status is OrderStatus.Delivered or OrderStatus.Cancelled;
    }

    private static string GetInvalidOrderStatusMessage(Order order, OrderStatus requestedStatus)
    {
        if (order.Status == OrderStatus.Pending &&
            requestedStatus == OrderStatus.Confirmed &&
            order.PaymentStatus != PaymentStatus.Paid)
        {
            return "Siparis onaylanmadan once odeme durumu odendi olmali.";
        }

        if (order.Status != OrderStatus.Pending &&
            requestedStatus == OrderStatus.Cancelled &&
            order.PaymentStatus == PaymentStatus.Paid)
        {
            return "Odenmis siparisi iptal etmek icin once odeme durumunu iade edildi yapmalisin.";
        }

        if (IsTerminal(order.Status))
        {
            return "Tamamlanmis veya iptal edilmis siparislerde durum degistirilemez.";
        }

        return "Bu siparis durumu gecisine izin verilmiyor.";
    }

    private static string GetInvalidPaymentStatusMessage(
        Order order,
        PaymentStatus requestedPaymentStatus)
    {
        if (order.Status == OrderStatus.Delivered && requestedPaymentStatus == PaymentStatus.Refunded)
        {
            return "Teslim edilmis siparis iade edildi durumuna alinamaz.";
        }

        if (IsTerminal(order.Status))
        {
            return "Tamamlanmis veya iptal edilmis siparislerde odeme durumu degistirilemez.";
        }

        if (order.PaymentStatus == PaymentStatus.Refunded)
        {
            return "Iade edilmis odeme durumu degistirilemez.";
        }

        return "Bu odeme durumu gecisine izin verilmiyor.";
    }
}

public record OrderWorkflowResult(
    bool Succeeded,
    int? OrderId,
    string? Message)
{
    public static OrderWorkflowResult Ok(int orderId)
    {
        return new OrderWorkflowResult(true, orderId, null);
    }

    public static OrderWorkflowResult Fail(string message)
    {
        return new OrderWorkflowResult(false, null, message);
    }
}
