using Microsoft.EntityFrameworkCore;
using NO23.Web.Data;
using NO23.Web.Domain.Enums;
using NO23.Web.Services;

namespace NO23.Web.Services.Payments;

public sealed class IyzicoPendingPaymentService(
    ApplicationDbContext dbContext,
    IIyzicoCheckoutClient checkoutClient,
    KitchenPlanMatchingService kitchenPlanMatchingService,
    ILogger<IyzicoPendingPaymentService> logger,
    ShopStockNotificationService? shopStockNotificationService = null)
{
    private const string ProviderName = "iyzico";
    private const string MissingPaymentErrorCode = "5122";
    private const int BatchSize = 50;
    private const int LastErrorMaximumLength = 2000;
    private static readonly TimeSpan OrphanShopOrderExpiration =
        TimeSpan.FromMinutes(30);

    public async Task<int> ProcessExpiredPaymentsAsync(
        CancellationToken cancellationToken = default)
    {
        var utcNow = DateTime.UtcNow;

        var paymentIds = await dbContext.PaymentTransactions
            .AsNoTracking()
            .Where(payment =>
                payment.Provider == ProviderName &&
                payment.PaymentStatus == PaymentStatus.Pending &&
                payment.CheckoutExpiresAtUtc.HasValue &&
                payment.CheckoutExpiresAtUtc.Value <= utcNow &&
                !string.IsNullOrWhiteSpace(payment.Token))
            .OrderBy(payment => payment.CheckoutExpiresAtUtc)
            .Select(payment => payment.Id)
            .Take(BatchSize)
            .ToListAsync(cancellationToken);

        var processedCount = 0;

        foreach (var paymentId in paymentIds)
        {
            var processed = await ReconcileAsync(
                paymentId,
                cancellationToken);

            if (processed)
            {
                processedCount++;
            }
        }

        return processedCount;
    }

    public async Task<int> ProcessStaleOrphanShopOrdersAsync(
        CancellationToken cancellationToken = default)
    {
        var utcNow =
            DateTime.UtcNow;

        var expiresBeforeUtc =
            utcNow.Subtract(
                OrphanShopOrderExpiration);

        var staleOrders =
            await dbContext.Orders
                .Include(order => order.Items)
                    .ThenInclude(item => item.ShopProduct)
                .Include(order => order.PaymentTransactions)
                .Where(order =>
                    order.Status == OrderStatus.Pending &&
                    order.PaymentStatus == PaymentStatus.Pending &&
                    order.CreatedAtUtc <= expiresBeforeUtc &&
                    !order.StockRestoredAtUtc.HasValue &&
                    !order.PaymentTransactions.Any() &&
                    order.Items.Any(item =>
                        item.ItemType == CartItemType.ShopProduct &&
                        item.ShopProductId.HasValue))
                .OrderBy(order => order.CreatedAtUtc)
                .Take(BatchSize)
                .ToListAsync(cancellationToken);

        foreach (var order in staleOrders)
        {
            MarkOrphanShopOrderAsExpired(
                order,
                utcNow);
        }

        if (staleOrders.Count > 0)
        {
            await dbContext.SaveChangesAsync(
                cancellationToken);
        }

        return staleOrders.Count;
    }

    private async Task<bool> ReconcileAsync(
        int paymentTransactionId,
        CancellationToken cancellationToken)
    {
        var paymentTransaction =
            await dbContext.PaymentTransactions
                .Include(payment => payment.Order)
                    .ThenInclude(order => order.Items)
                        .ThenInclude(item => item.ShopProduct)
                .FirstOrDefaultAsync(
                    payment =>
                        payment.Id == paymentTransactionId,
                    cancellationToken);

        if (paymentTransaction is null)
        {
            return false;
        }

        var order = paymentTransaction.Order;

        // Callback daha önce işlemi tamamladıysa tekrar dokunma.
        if (paymentTransaction.PaymentStatus != PaymentStatus.Pending ||
            order.PaymentStatus != PaymentStatus.Pending)
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(paymentTransaction.Token))
        {
            return false;
        }

        IyzicoCheckoutRetrieveResult retrieveResult;

        try
        {
            retrieveResult =
                await checkoutClient.RetrieveAsync(
                    paymentTransaction.ConversationId,
                    paymentTransaction.Token,
                    cancellationToken);
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "Süresi dolmuş iyzico ödemesi sorgulanırken hata oluştu. OrderId: {OrderId}, PaymentTransactionId: {PaymentTransactionId}",
                order.Id,
                paymentTransaction.Id);

            // iyzico'ya erişememek, ödemenin başarısız
            // veya expired olduğu anlamına gelmez.
            paymentTransaction.LastError =
                TruncateLastError(exception.Message);

            paymentTransaction.UpdatedAtUtc =
                DateTime.UtcNow;

            await dbContext.SaveChangesAsync(
                CancellationToken.None);

            return false;
        }

        var checkedAtUtc = DateTime.UtcNow;

        paymentTransaction.RawStatus =
            retrieveResult.RawStatus;

        paymentTransaction.PaymentId =
            retrieveResult.PaymentId;

        paymentTransaction.FraudStatus =
            retrieveResult.FraudStatus;

        paymentTransaction.RawRetrieveResponseJson =
            retrieveResult.RawResponseJson;

        paymentTransaction.UpdatedAtUtc =
            checkedAtUtc;

        // Provider isteği işleyememişse şimdilik Pending bırakıyoruz.
        //
        // Gerçek Order 22 üzerinde bunun iyzico tarafından nasıl
        // döndüğünü birazdan göreceğiz. Burada tahmin ederek
        // stoğu geri vermiyoruz.


        // Yanlış işlem sonucuyla Order güncellenmesini engelle.
        if (!string.IsNullOrWhiteSpace(
                retrieveResult.ConversationId) &&
            !string.Equals(
                retrieveResult.ConversationId,
                paymentTransaction.ConversationId,
                StringComparison.Ordinal))
        {
            paymentTransaction.LastError =
                "iyzico ConversationId eşleşmedi.";

            await dbContext.SaveChangesAsync(
                cancellationToken);

            return false;
        }

        if (!string.IsNullOrWhiteSpace(
                retrieveResult.BasketId) &&
            !string.Equals(
                retrieveResult.BasketId,
                paymentTransaction.BasketId,
                StringComparison.Ordinal))
        {
            paymentTransaction.LastError =
                "iyzico BasketId eşleşmedi.";

            await dbContext.SaveChangesAsync(
                cancellationToken);

            return false;
        }

            if (string.Equals(
                retrieveResult.PaymentStatus,
                "SUCCESS",
                StringComparison.OrdinalIgnoreCase))
        {
            MarkAsPaid(
                paymentTransaction,
                order,
                checkedAtUtc);

            await dbContext.SaveChangesAsync(
                cancellationToken);

            if (shopStockNotificationService is not null)
            {
                await shopStockNotificationService
                    .PublishForPaidOrderAsync(
                        order.Id,
                        cancellationToken);
            }


            if (order.Type == OrderType.KitchenSubscription)
            {
                var activated =
                    await ActivateKitchenPackageAsync(
                        order,
                        cancellationToken);

                if (!activated)
                {
                    logger.LogError(
                        "Pending iyzico ödemesi başarılı bulundu ancak Kitchen paketi aktifleştirilemedi. OrderId: {OrderId}, KitchenSubscriptionId: {KitchenSubscriptionId}",
                        order.Id,
                        order.KitchenSubscriptionId);
                }
            }

            return true;
        }

            if (string.Equals(
                retrieveResult.PaymentStatus,
                "FAILURE",
                StringComparison.OrdinalIgnoreCase))
        {
            MarkAsFailed(
                paymentTransaction,
                order,
                checkedAtUtc);

            if (order.Type == OrderType.KitchenSubscription)
            {
                await MarkKitchenPaymentFailedAsync(
                    order,
                    checkedAtUtc,
                    cancellationToken);
            }

            await dbContext.SaveChangesAsync(
                cancellationToken);

            return true;
        }

            if (!retrieveResult.Succeeded &&
                IsTerminalExpiredCheckoutFailure(
                    retrieveResult))
        {
            MarkAsExpired(
                paymentTransaction,
                order,
                checkedAtUtc,
                BuildProviderFailureStatus(
                    retrieveResult));

            if (order.Type == OrderType.KitchenSubscription)
            {
                await MarkKitchenCheckoutExpiredAsync(
                    order,
                    checkedAtUtc,
                    cancellationToken);
            }

            await dbContext.SaveChangesAsync(
                cancellationToken);

            return true;
        }

            if (!retrieveResult.Succeeded)
        {
            var errorMessage =
                string.IsNullOrWhiteSpace(retrieveResult.ErrorMessage)
                    ? "iyzico ödeme sonucu alınamadı."
                    : retrieveResult.ErrorMessage;

            paymentTransaction.LastError =
                TruncateLastError(
                    string.IsNullOrWhiteSpace(retrieveResult.ErrorCode)
                        ? errorMessage
                        : $"{retrieveResult.ErrorCode}: {errorMessage}");

            await dbContext.SaveChangesAsync(
                cancellationToken);

            return false;
        }

        // Retrieve isteği başarılı fakat artık tamamlanmış
        // bir ödeme sonucu yok.
        //
        // Checkout süresi zaten dolmuş olduğundan bu ödeme
        // rezervasyonunu Expired kabul ediyoruz.
        MarkAsExpired(
            paymentTransaction,
            order,
            checkedAtUtc,
            retrieveResult.PaymentStatus);

        if (order.Type == OrderType.KitchenSubscription)
        {
            await MarkKitchenCheckoutExpiredAsync(
                order,
                checkedAtUtc,
                cancellationToken);
        }

        await dbContext.SaveChangesAsync(
            cancellationToken);

        return true;
    }

    private static bool IsTerminalExpiredCheckoutFailure(
        IyzicoCheckoutRetrieveResult retrieveResult)
    {
        return string.Equals(
            retrieveResult.ErrorCode?.Trim(),
            MissingPaymentErrorCode,
            StringComparison.OrdinalIgnoreCase);
    }

    private static string BuildProviderFailureStatus(
        IyzicoCheckoutRetrieveResult retrieveResult)
    {
        if (string.IsNullOrWhiteSpace(
                retrieveResult.ErrorCode))
        {
            return retrieveResult.PaymentStatus ??
                   "UNKNOWN";
        }

        return string.IsNullOrWhiteSpace(
                retrieveResult.ErrorMessage)
            ? $"ERROR {retrieveResult.ErrorCode}"
            : $"ERROR {retrieveResult.ErrorCode}: {retrieveResult.ErrorMessage}";
    }

    private async Task<bool> ActivateKitchenPackageAsync(
    Domain.Entities.Order order,
    CancellationToken cancellationToken)
    {
        if (!order.KitchenSubscriptionId.HasValue)
        {
            return false;
        }

        var subscription =
            await dbContext.KitchenSubscriptions
                .Include(item => item.MealPlan)
                .FirstOrDefaultAsync(
                    item =>
                        item.Id ==
                        order.KitchenSubscriptionId.Value,
                    cancellationToken);

        if (subscription is null)
        {
            return false;
        }

        if (subscription.Status ==
                KitchenSubscriptionStatus.Active &&
            subscription.MealPlan is not null)
        {
            return true;
        }

        if (!subscription.SourceHeightCm.HasValue ||
            !subscription.SourceWeightKg.HasValue ||
            !subscription.SourceAge.HasValue ||
            !subscription.SourceGender.HasValue ||
            !subscription.SourceActivityLevel.HasValue)
        {
            logger.LogError(
                "Kitchen paketi için kalori kaynak bilgileri eksik. KitchenSubscriptionId: {KitchenSubscriptionId}",
                subscription.Id);

            return false;
        }

        if (subscription.PackageDaysSnapshot <= 0)
        {
            logger.LogError(
                "Kitchen paket gün sayısı geçersiz. KitchenSubscriptionId: {KitchenSubscriptionId}",
                subscription.Id);

            return false;
        }

        var startsOn =
            DateOnly.FromDateTime(
                DateTime.Today.AddDays(1));

        subscription.StartsOn =
            startsOn;

        subscription.EndsOn =
            startsOn.AddDays(
                subscription.PackageDaysSnapshot - 1);

        subscription.UpdatedAtUtc =
            DateTime.UtcNow;

        await dbContext.SaveChangesAsync(
            cancellationToken);

        var calculationRequest =
            new CalorieCalculationRequest
            {
                HeightCm =
                    subscription.SourceHeightCm.Value,

                WeightKg =
                    subscription.SourceWeightKg.Value,

                Age =
                    subscription.SourceAge.Value,

                Gender =
                    subscription.SourceGender.Value,

                ActivityLevel =
                    subscription.SourceActivityLevel.Value,

                Goal =
                    subscription.Goal
            };

        var planResult =
            await kitchenPlanMatchingService.GenerateAsync(
                subscription.Id,
                calculationRequest);

        if (!planResult.Succeeded)
        {
            subscription.Status =
                KitchenSubscriptionStatus.Paused;

            subscription.UpdatedAtUtc =
                DateTime.UtcNow;

            await dbContext.SaveChangesAsync(
                cancellationToken);

            logger.LogError(
                "Pending ödeme başarılı bulundu ancak Kitchen planı üretilemedi. KitchenSubscriptionId: {KitchenSubscriptionId}, Error: {Error}",
                subscription.Id,
                planResult.Message);

            return false;
        }

        subscription.Status =
            KitchenSubscriptionStatus.Active;

        subscription.UpdatedAtUtc =
            DateTime.UtcNow;

        await dbContext.SaveChangesAsync(
            cancellationToken);

        return true;
    }    

    private async Task MarkKitchenPaymentFailedAsync(
    Domain.Entities.Order order,
    DateTime utcNow,
    CancellationToken cancellationToken)
    {
        if (!order.KitchenSubscriptionId.HasValue)
        {
            return;
        }

        var subscription =
            await dbContext.KitchenSubscriptions
                .FirstOrDefaultAsync(
                    item =>
                        item.Id ==
                        order.KitchenSubscriptionId.Value,
                    cancellationToken);

        if (subscription is null)
        {
            return;
        }

        if (subscription.Status ==
            KitchenSubscriptionStatus.PendingPayment)
        {
            subscription.Status =
                KitchenSubscriptionStatus.PaymentFailed;

            subscription.UpdatedAtUtc =
                utcNow;
        }
    }

    private async Task MarkKitchenCheckoutExpiredAsync(
    Domain.Entities.Order order,
    DateTime utcNow,
    CancellationToken cancellationToken)
    {
        if (!order.KitchenSubscriptionId.HasValue)
        {
            return;
        }

        var subscription =
            await dbContext.KitchenSubscriptions
                .FirstOrDefaultAsync(
                    item =>
                        item.Id ==
                        order.KitchenSubscriptionId.Value,
                    cancellationToken);

        if (subscription is null)
        {
            return;
        }

        if (subscription.Status ==
            KitchenSubscriptionStatus.PendingPayment)
        {
            subscription.Status =
                KitchenSubscriptionStatus.Cancelled;

            subscription.UpdatedAtUtc =
                utcNow;
        }
    }


    private static void MarkAsPaid(
        Domain.Entities.PaymentTransaction paymentTransaction,
        Domain.Entities.Order order,
        DateTime utcNow)
    {
        paymentTransaction.PaymentStatus =
            PaymentStatus.Paid;

        paymentTransaction.CompletedAtUtc =
            utcNow;

        paymentTransaction.FailedAtUtc =
            null;

        paymentTransaction.ExpiredAtUtc =
            null;

        paymentTransaction.LastError =
            null;

        paymentTransaction.UpdatedAtUtc =
            utcNow;

        order.PaymentStatus =
            PaymentStatus.Paid;

        order.Status =
            OrderStatus.Confirmed;

        order.UpdatedAtUtc =
            utcNow;
    }

    private static void MarkAsFailed(
        Domain.Entities.PaymentTransaction paymentTransaction,
        Domain.Entities.Order order,
        DateTime utcNow)
    {
        paymentTransaction.PaymentStatus =
            PaymentStatus.Failed;

        paymentTransaction.FailedAtUtc =
            utcNow;

        paymentTransaction.ExpiredAtUtc =
            null;

        paymentTransaction.LastError =
            TruncateLastError(
                "iyzico ödeme sonucu FAILURE.");

        paymentTransaction.UpdatedAtUtc =
            utcNow;

        order.PaymentStatus =
            PaymentStatus.Failed;

        order.Status =
            OrderStatus.Cancelled;

        order.UpdatedAtUtc =
            utcNow;

        OrderWorkflowService.RestoreShopProductStockOnce(
            order);
    }

    private static void MarkAsExpired(
        Domain.Entities.PaymentTransaction paymentTransaction,
        Domain.Entities.Order order,
        DateTime utcNow,
        string? providerPaymentStatus)
    {
        paymentTransaction.PaymentStatus =
            PaymentStatus.Expired;

        paymentTransaction.ExpiredAtUtc =
            utcNow;

        paymentTransaction.FailedAtUtc =
            null;

        paymentTransaction.LastError =
            TruncateLastError(
                $"iyzico checkout süresi doldu. PaymentStatus: {providerPaymentStatus ?? "UNKNOWN"}.");

        paymentTransaction.UpdatedAtUtc =
            utcNow;

        order.PaymentStatus =
            PaymentStatus.Expired;

        order.Status =
            OrderStatus.Cancelled;

        order.UpdatedAtUtc =
            utcNow;

        OrderWorkflowService.RestoreShopProductStockOnce(
            order);
    }

    private static void MarkOrphanShopOrderAsExpired(
        Domain.Entities.Order order,
        DateTime utcNow)
    {
        order.PaymentStatus =
            PaymentStatus.Expired;

        order.Status =
            OrderStatus.Cancelled;

        order.UpdatedAtUtc =
            utcNow;

        OrderWorkflowService.RestoreShopProductStockOnce(
            order);
    }

    private static string TruncateLastError(
        string message)
    {
        return message.Length <= LastErrorMaximumLength
            ? message
            : message[..LastErrorMaximumLength];
    }
}
