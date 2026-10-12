using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using NO23.Web.Data;
using NO23.Web.Domain.Entities;
using NO23.Web.Domain.Enums;
using NO23.Web.ViewModels.Admin;

namespace NO23.Web.Services;

public sealed class ManualOrderPaymentService(ApplicationDbContext dbContext, ILogger<ManualOrderPaymentService>? logger = null)
{
    public static decimal PackageTotal(ServicePackageVariant variant) => variant.BillingType == ServicePackageBillingType.MonthlySubscription
        ? (variant.MonthlyPrice ?? 0) * (variant.DurationMonths ?? 1) : variant.TotalPrice;

    public static bool IsMethodAllowed(Order order, PaymentMethod method) => Enum.IsDefined(method) &&
        (method == PaymentMethod.CreditCard || (order.Type != OrderType.MembershipRenewal &&
            order.ServicePackageVariant?.ServicePackage.Category != ServicePackageCategory.Membership));

    public static void ConfigureOfflineOrder(Order order)
    {
        if (order.PaymentMethod == PaymentMethod.CreditCard) return;
        if (!IsMethodAllowed(order, order.PaymentMethod))
            throw new InvalidOperationException("Bu paket için kredi kartı zorunludur.");
        order.IsManualPayment = true;
        order.PaymentStatus = PaymentStatus.Pending;
        order.PaymentInstallments = [new OrderPaymentInstallment
        {
            Number = 1, Amount = order.Total, DueDate = DateOnly.FromDateTime(ClubTime.Now)
        }];
    }

    public static (Order? Order, IReadOnlyList<ValidationResult> Errors) PreparePackageOrder(
        ServicePackageVariant variant, ManualPaymentInputViewModel input, int discountPercent = 0,
        string? recordedByUserId = null)
    {
        var errors = new List<ValidationResult>();
        var today = DateOnly.FromDateTime(ClubTime.Now);
        var expectedAmount = Math.Round(PackageTotal(variant) * (100 - discountPercent) / 100m, 2);
        var amount = input.Amount ?? expectedAmount;
        if (!Enum.IsDefined(input.Method) || (variant.ServicePackage.Category == ServicePackageCategory.Membership && input.Method != PaymentMethod.CreditCard))
            errors.Add(new("Üyelik paketlerinde kredi kartı zorunludur; diğer paketlerde geçerli bir ödeme yöntemi seçin.", ["Payment.Method"]));
        if (amount is <= 0 or > 99999999.99m || decimal.Round(amount, 2) != amount)
            errors.Add(new("Ödeme tutarı sıfırdan büyük olmalı ve en fazla iki ondalık içermelidir.", ["Payment.Amount"]));
        else if (!variant.PriceOnRequest && amount != expectedAmount)
            errors.Add(new("Ödeme tutarı seçilen paketin dönem toplamıyla eşleşmelidir.", ["Payment.Amount"]));
        if (input.InstallmentCount is < 1 or > 36)
            errors.Add(new("Taksit sayısı 1 ile 36 arasında olmalıdır.", ["Payment.InstallmentCount"]));
        if (input.FirstDueDate < today.AddYears(-10) || input.FirstDueDate > today.AddYears(5))
            errors.Add(new("İlk vade tarihini kontrol edin.", ["Payment.FirstDueDate"]));
        if (input.Reference?.Length > 160)
            errors.Add(new("Ödeme referansı en fazla 160 karakter olabilir.", ["Payment.Reference"]));
        if (errors.Count > 0) return (null, errors);

        var installments = input.Installments.Count == 0
            ? BuildInstallments(amount, input.InstallmentCount, input.FirstDueDate)
            : input.Installments;
        if (installments.Count != input.InstallmentCount)
            errors.Add(new("Taksit satırlarının sayısı taksit sayısıyla eşleşmelidir.", ["Payment.InstallmentCount"]));
        for (var index = 0; index < installments.Count; index++)
        {
            var row = installments[index];
            if (row.Amount is null or <= 0 || row.Amount > 99999999.99m || decimal.Round(row.Amount.Value, 2) != row.Amount)
                errors.Add(new("Taksit tutarı sıfırdan büyük ve en fazla iki ondalık olmalıdır.", [$"Payment.Installments[{index}].Amount"]));
            if (row.DueDate < today.AddYears(-10) || row.DueDate > today.AddYears(5) ||
                (index > 0 && row.DueDate < installments[index - 1].DueDate))
                errors.Add(new("Taksit vadeleri geçerli ve tarih sırasına uygun olmalıdır.", [$"Payment.Installments[{index}].DueDate"]));
            if (row.PaidOn.HasValue && (row.PaidOn > today || row.PaidOn < today.AddYears(-10)))
                errors.Add(new("Tahsilat tarihi gelecekte olamaz.", [$"Payment.Installments[{index}].PaidOn"]));
        }
        if (installments.Sum(row => row.Amount ?? 0) != amount)
            errors.Add(new("Taksit tutarlarının toplamı ödeme tutarıyla eşleşmelidir.", ["Payment.Installments"]));
        if (errors.Count > 0) return (null, errors);

        return (new Order
        {
            OrderNumber = $"NO23-PK-{Guid.NewGuid():N}"[..40],
            Type = OrderType.ServicePackagePurchase,
            ServicePackageVariantId = variant.Id,
            ServicePackageVariant = variant,
            PaymentMethod = input.Method,
            IsManualPayment = true,
            PaymentReference = input.Reference?.Trim(),
            PaymentRecordedByUserId = recordedByUserId,
            PaymentStatus = installments.All(row => row.PaidOn.HasValue) ? PaymentStatus.Paid : PaymentStatus.Pending,
            Status = OrderStatus.Confirmed,
            DeliveryMethod = OrderDeliveryMethod.ClubPickup,
            DeliveryAddressLine = "Kulüp paket kaydı", DeliveryDistrict = "-", DeliveryCity = "-",
            Subtotal = amount, Total = amount,
            Items = [new OrderItem { ItemType = CartItemType.MembershipPackage,
                ProductName = $"{variant.ServicePackage.Name} — {variant.Name}", Quantity = 1,
                UnitPrice = amount, LineTotal = amount }],
            PaymentInstallments = installments.Select((row, index) => new OrderPaymentInstallment
            {
                Number = index + 1, Amount = row.Amount!.Value, DueDate = row.DueDate, PaidOn = row.PaidOn,
                PaymentReference = row.PaidOn.HasValue ? input.Reference?.Trim() : null,
                RecordedByUserId = row.PaidOn.HasValue ? recordedByUserId : null
            }).ToList()
        }, []);
    }

    public static List<PaymentInstallmentInputViewModel> BuildInstallments(decimal amount, int count, DateOnly firstDueDate)
    {
        var regular = decimal.Round(amount / count, 2, MidpointRounding.ToZero);
        return Enumerable.Range(0, count).Select(index => new PaymentInstallmentInputViewModel
        {
            Amount = index == count - 1 ? amount - regular * (count - 1) : regular,
            DueDate = firstDueDate.AddMonths(index)
        }).ToList();
    }

    public async Task<string?> RecordInstallmentAsync(int orderId, int installmentId, DateOnly paidOn,
        string? reference, string? recordedByUserId)
    {
        Order? order;
        await using (var transaction = await BeginOrderPaymentTransactionAsync(orderId))
        {
            order = await LoadPaymentOrderAsync(orderId);
            if (order is null) return "Manuel ödeme kaydı bulunamadı.";
            if (order.Status == OrderStatus.Cancelled || order.PaymentStatus == PaymentStatus.Refunded)
                return "İptal veya iade edilen kayıt için tahsilat girilemez.";
            var installment = order.PaymentInstallments.FirstOrDefault(item => item.Id == installmentId);
            if (installment is null) return "Taksit bulunamadı.";
            var changed = false;
            if (!installment.PaidOn.HasValue)
            {
                var today = DateOnly.FromDateTime(ClubTime.Now);
                if (paidOn > today || paidOn < today.AddYears(-10)) return "Geçerli bir tahsilat tarihi girin.";
                if (reference?.Length > 160) return "Ödeme referansı en fazla 160 karakter olabilir.";
                installment.PaidOn = paidOn;
                installment.PaymentReference = reference?.Trim();
                installment.RecordedByUserId = recordedByUserId;
                changed = true;
            }
            changed |= ReconcilePaymentStatus(order);
            if (changed)
            {
                order.UpdatedAtUtc = DateTime.UtcNow;
                await dbContext.SaveChangesAsync();
            }
            // Receipts must remain saved even when Kitchen plan generation fails.
            if (transaction is not null) await transaction.CommitAsync();
        }
        return await ActivatePaidServicesAsync(order);
    }

    public async Task<string?> RetryActivationAsync(int orderId)
    {
        Order? order;
        await using (var transaction = await BeginOrderPaymentTransactionAsync(orderId))
        {
            order = await LoadPaymentOrderAsync(orderId);
            if (order is null || order.Type != OrderType.KitchenSubscription || order.KitchenSubscriptionId is null)
                return "Manuel Kitchen ödeme kaydı bulunamadı.";
            if (order.Status == OrderStatus.Cancelled || order.PaymentStatus == PaymentStatus.Refunded)
                return "İptal veya iade edilen Kitchen paketi aktifleştirilemez.";
            if (order.PaymentInstallments.Count == 0 || order.PaymentInstallments.Any(item => !item.PaidOn.HasValue))
                return "Kitchen paketi aktifleştirilmeden önce tüm taksitler tahsil edilmelidir.";
            if (ReconcilePaymentStatus(order))
            {
                order.UpdatedAtUtc = DateTime.UtcNow;
                await dbContext.SaveChangesAsync();
            }
            if (transaction is not null) await transaction.CommitAsync();
        }
        return await ActivatePaidServicesAsync(order);
    }

    private async Task<IDbContextTransaction?> BeginOrderPaymentTransactionAsync(int orderId)
    {
        if (!dbContext.Database.IsRelational()) return null;
        var transaction = await dbContext.Database.BeginTransactionAsync();
        try
        {
            // Lock before reading installment rows so concurrent receipts see the last committed schedule.
            await dbContext.Database.ExecuteSqlInterpolatedAsync(
                $"SELECT 1 FROM \"Orders\" WHERE \"Id\" = {orderId} FOR UPDATE");
            return transaction;
        }
        catch
        {
            await transaction.DisposeAsync();
            throw;
        }
    }

    private async Task<Order?> LoadPaymentOrderAsync(int orderId)
    {
        var order = await dbContext.Orders.Include(item => item.PaymentInstallments)
            .FirstOrDefaultAsync(item => item.Id == orderId && item.IsManualPayment);
        if (order is null) return null;
        // Refresh objects already tracked by a caller; the lock is held while these rows are read.
        await dbContext.Entry(order).ReloadAsync();
        foreach (var installment in order.PaymentInstallments)
            await dbContext.Entry(installment).ReloadAsync();
        return order.IsManualPayment ? order : null;
    }

    private static bool ReconcilePaymentStatus(Order order)
    {
        var status = order.PaymentInstallments.Count > 0 && order.PaymentInstallments.All(item => item.PaidOn.HasValue)
            ? PaymentStatus.Paid : PaymentStatus.Pending;
        if (order.PaymentStatus == status) return false;
        order.PaymentStatus = status;
        return true;
    }

    private async Task<string?> ActivatePaidServicesAsync(Order order)
    {
        if (order.PaymentStatus != PaymentStatus.Paid || order.Type != OrderType.KitchenSubscription || order.KitchenSubscriptionId is null)
            return null;
        try
        {
            return await ActivateKitchenPackageAsync(order.KitchenSubscriptionId.Value);
        }
        catch (Exception exception)
        {
            logger?.LogError(exception, "Manual Kitchen payment was saved but activation failed. OrderId: {OrderId}", order.Id);
            return "Tahsilat kaydedildi; Kitchen planı hazırlanırken bir sorun oluştu. Ödeme ekranından aktifleştirmeyi yeniden deneyin.";
        }
    }

    private async Task<string?> ActivateKitchenPackageAsync(int subscriptionId)
    {
        await using var transaction = dbContext.Database.IsRelational()
            ? await dbContext.Database.BeginTransactionAsync() : null;
        if (transaction is not null)
            await dbContext.Database.ExecuteSqlInterpolatedAsync(
                $"SELECT 1 FROM \"KitchenSubscriptions\" WHERE \"Id\" = {subscriptionId} FOR UPDATE");
        var subscription = await dbContext.KitchenSubscriptions.Include(item => item.MealPlan)
            .FirstOrDefaultAsync(item => item.Id == subscriptionId);
        if (subscription is null) return "Tahsilat kaydedildi; Kitchen aboneliği bulunamadı.";
        await dbContext.Entry(subscription).ReloadAsync();
        subscription.MealPlan = await dbContext.KitchenMealPlans
            .FirstOrDefaultAsync(item => item.KitchenSubscriptionId == subscriptionId);
        if (subscription.MealPlan is not null)
            await dbContext.Entry(subscription.MealPlan).ReloadAsync();
        if (subscription.MealPlan?.Status == KitchenMealPlanStatus.Generated)
        {
            // An existing valid plan retains its dates and contents when activation is retried.
            if (subscription.Status != KitchenSubscriptionStatus.Active)
            {
                subscription.Status = KitchenSubscriptionStatus.Active;
                subscription.UpdatedAtUtc = DateTime.UtcNow;
                await dbContext.SaveChangesAsync();
            }
            if (transaction is not null) await transaction.CommitAsync();
            return null;
        }
        if (subscription.PackageDaysSnapshot <= 0 || (subscription.DailyCalories > 0 &&
            (!subscription.SourceHeightCm.HasValue || !subscription.SourceWeightKg.HasValue ||
                !subscription.SourceAge.HasValue || !subscription.SourceGender.HasValue || !subscription.SourceActivityLevel.HasValue)))
            return "Tahsilat kaydedildi; Kitchen planı için paket / kalori bilgilerini tamamlayın.";
        if (subscription.MealPlan?.Status == KitchenMealPlanStatus.Failed)
        {
            // GenerateAsync reuses existing plans, so discard a failed draft before rebuilding.
            await dbContext.Entry(subscription.MealPlan).Collection(item => item.Days).Query()
                .Include(item => item.Items).LoadAsync();
            dbContext.KitchenMealPlans.Remove(subscription.MealPlan);
            subscription.MealPlan = null;
        }
        var startsOn = DateOnly.FromDateTime(ClubTime.Now).AddDays(1);
        subscription.StartsOn = startsOn;
        subscription.EndsOn = startsOn.AddDays(subscription.PackageDaysSnapshot - 1);
        subscription.UpdatedAtUtc = DateTime.UtcNow;
        CalorieCalculationRequest? request = subscription.DailyCalories > 0 ? new()
        {
            HeightCm = subscription.SourceHeightCm!.Value, WeightKg = subscription.SourceWeightKg!.Value,
            Age = subscription.SourceAge!.Value, Gender = subscription.SourceGender!.Value,
            ActivityLevel = subscription.SourceActivityLevel!.Value, Goal = subscription.Goal
        } : null;
        await dbContext.SaveChangesAsync();
        var result = await new KitchenPlanMatchingService(dbContext).GenerateAsync(subscription.Id, request);
        subscription.Status = result.Succeeded ? KitchenSubscriptionStatus.Active : KitchenSubscriptionStatus.Paused;
        await dbContext.SaveChangesAsync();
        if (transaction is not null) await transaction.CommitAsync();
        return result.Succeeded ? null : $"Tahsilat kaydedildi; Kitchen planı hazırlanamadı: {result.Message}";
    }
}
