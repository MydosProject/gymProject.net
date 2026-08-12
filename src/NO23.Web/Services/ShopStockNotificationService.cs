using Microsoft.EntityFrameworkCore;
using NO23.Web.Data;
using NO23.Web.Domain.Enums;

namespace NO23.Web.Services;

public sealed class ShopStockNotificationService(
    ApplicationDbContext dbContext,
    AdminStockNotificationService adminStockNotificationService)
{
    public async Task PublishForPaidOrderAsync(
        int orderId,
        CancellationToken cancellationToken = default)
    {
        var order = await dbContext.Orders
            .AsNoTracking()
            .Include(order => order.Items)
            .FirstOrDefaultAsync(
                order => order.Id == orderId,
                cancellationToken);

        if (order is null ||
            order.PaymentStatus != PaymentStatus.Paid)
        {
            return;
        }

        var paidShopItems = order.Items
            .Where(item =>
                item.ItemType == CartItemType.ShopProduct &&
                item.ShopProductId.HasValue)
            .GroupBy(item => item.ShopProductId!.Value)
            .Select(group => new
            {
                ProductId = group.Key,
                Quantity = group.Sum(item => item.Quantity)
            })
            .ToList();

        foreach (var paidShopItem in paidShopItems)
        {
            var product = await dbContext.ShopProducts
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    product =>
                        product.Id == paidShopItem.ProductId,
                    cancellationToken);

            if (product is null)
            {
                continue;
            }

            /*
             * StockQuantity şu anda:
             *
             * gerçek satışlar
             * +
             * henüz ödeme bekleyen rezervasyonlar
             *
             * nedeniyle düşmüş olabilir.
             *
             * Pending rezervasyonları geri ekleyerek
             * gerçek satılmış stok seviyesini buluyoruz.
             */
            var pendingReservedQuantity =
                await dbContext.OrderItems
                    .AsNoTracking()
                    .Where(item =>
                        item.ShopProductId ==
                            paidShopItem.ProductId &&
                        item.ItemType ==
                            CartItemType.ShopProduct &&
                        item.Order.PaymentStatus ==
                            PaymentStatus.Pending &&
                        item.Order.Status ==
                            OrderStatus.Pending &&
                        !item.Order.StockRestoredAtUtc.HasValue)
                    .Select(item => (int?)item.Quantity)
                    .SumAsync(cancellationToken)
                ?? 0;

            var currentCommittedQuantity =
                product.StockQuantity +
                pendingReservedQuantity;

            var previousCommittedQuantity =
                currentCommittedQuantity +
                paidShopItem.Quantity;

            await adminStockNotificationService
                .PublishShopStockChangedAsync(
                    product.Id,
                    product.Name,
                    previousCommittedQuantity,
                    currentCommittedQuantity,
                    product.MinimumStockQuantity);
        }
    }
}