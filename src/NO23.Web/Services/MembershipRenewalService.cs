using Microsoft.EntityFrameworkCore;
using NO23.Web.Data;
using NO23.Web.Domain.Entities;
using NO23.Web.Domain.Enums;

namespace NO23.Web.Services;

public sealed class MembershipRenewalService(ApplicationDbContext dbContext)
{
    public async Task<(int? OrderId, string? Error)> CreateOrderAsync(
        string userId, int variantId, string fullName, string phone,
        string address, string district, string city,
        CancellationToken cancellationToken = default)
    {
        var profile = await dbContext.MemberProfiles
            .FirstOrDefaultAsync(x => x.ApplicationUserId == userId, cancellationToken);
        if (profile is null)
            return (null, "Üye profili bulunamadı.");

        if (profile.MembershipEndsAtUtc > DateTime.UtcNow)
            return (null, "Mevcut üyeliğin henüz bitmedi. Yenileme, bitiş tarihinden sonra açılır.");

        var pendingOrder = await dbContext.Orders
            .Where(x => x.MemberProfileId == profile.Id &&
                x.Type == OrderType.MembershipRenewal &&
                x.PaymentStatus == PaymentStatus.Pending && x.Status == OrderStatus.Pending)
            .OrderByDescending(x => x.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);
        if (pendingOrder is not null)
        {
            if (pendingOrder.ServicePackageVariantId == variantId)
                return (pendingOrder.Id, null);
            return (null, "Devam eden bir üyelik ödemen var. Önce bu ödemeyi tamamla veya süresinin dolmasını bekle.");
        }

        var variant = await dbContext.ServicePackageVariants
            .Include(x => x.ServicePackage)
            .FirstOrDefaultAsync(x => x.Id == variantId && x.IsActive &&
                x.ServicePackage.IsActive &&
                x.ServicePackage.Category == ServicePackageCategory.Membership,
                cancellationToken);
        if (variant is null || variant.ServicePackage.MembershipPackageId is null ||
            variant.PriceOnRequest || variant.TotalPrice <= 0 ||
            (variant.DurationMonths is not > 0 && variant.DurationDays is not > 0 &&
             variant.BillingType != ServicePackageBillingType.MonthlySubscription))
            return (null, "Bu paket çevrimiçi yenilemeye uygun değil.");

        var order = new Order
        {
            OrderNumber = $"NO23-UY-{Guid.NewGuid():N}"[..32],
            MemberProfileId = profile.Id,
            ServicePackageVariantId = variant.Id,
            Type = OrderType.MembershipRenewal,
            DeliveryMethod = OrderDeliveryMethod.ClubPickup,
            DeliveryFullName = fullName.Trim(),
            DeliveryPhoneNumber = phone.Trim(),
            DeliveryAddressLine = address.Trim(),
            DeliveryDistrict = district.Trim(),
            DeliveryCity = city.Trim(),
            Subtotal = variant.TotalPrice,
            Total = variant.TotalPrice,
            Items = [new OrderItem
            {
                ItemType = CartItemType.MembershipPackage,
                ProductName = $"{variant.ServicePackage.Name} - {variant.Name}",
                UnitPrice = variant.TotalPrice,
                Quantity = 1,
                LineTotal = variant.TotalPrice
            }]
        };
        dbContext.Orders.Add(order);
        await dbContext.SaveChangesAsync(cancellationToken);
        return (order.Id, null);
    }

    public async Task<bool> ActivatePaidOrderAsync(int orderId,
        CancellationToken cancellationToken = default)
    {
        var order = await dbContext.Orders
            .Include(x => x.ServicePackageVariant)
                .ThenInclude(x => x!.ServicePackage)
            .FirstOrDefaultAsync(x => x.Id == orderId &&
                x.Type == OrderType.MembershipRenewal &&
                x.PaymentStatus == PaymentStatus.Paid,
                cancellationToken);
        if (order?.MemberProfileId is null || order.ServicePackageVariant is null ||
            order.ServicePackageVariant.ServicePackage.MembershipPackageId is null)
            return false;

        var profile = await dbContext.MemberProfiles
            .FirstOrDefaultAsync(x => x.Id == order.MemberProfileId, cancellationToken);
        if (profile is null)
            return false;
        if (profile.LastMembershipOrderId == order.Id)
            return true;
        if (profile.MembershipEndsAtUtc > order.CreatedAtUtc)
            return false;

        var variant = order.ServicePackageVariant;
        var startsAt = DateTime.UtcNow;
        var endsAt = MemberPackageEntitlement.CalculateEndDate(variant, startsAt);

        profile.MembershipPackageId = variant.ServicePackage.MembershipPackageId.Value;
        profile.MembershipPackageOptionId = null;
        profile.ServicePackageVariantId = variant.Id;
        profile.RemainingClassCredits = MemberPackageEntitlement.CalculateInitialCredits(variant);
        profile.MembershipStartsAtUtc = startsAt;
        profile.MembershipEndsAtUtc = endsAt;
        profile.LastMembershipOrderId = order.Id;
        profile.UpdatedAtUtc = startsAt;
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}
