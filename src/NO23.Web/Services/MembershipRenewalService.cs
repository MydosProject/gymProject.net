using Microsoft.EntityFrameworkCore;
using NO23.Web.Data;
using NO23.Web.Domain.Entities;
using NO23.Web.Domain.Enums;
using System.Data;
using System.Text.Json;

namespace NO23.Web.Services;

public sealed class MembershipRenewalService(ApplicationDbContext dbContext)
{
    private sealed record CreditSnapshot(int Version, int Pt, int Performance, int Reformer, int Group, int Kids,
        bool Monthly, int? Months, int? Days)
    {
        public static CreditSnapshot From(ServicePackageVariant v) => new(1, v.PersonalTrainingSessionCount, v.PerformanceClassCreditCount,
            v.ReformerClassCreditCount, v.GroupClassCreditCount, v.KidsClassCreditCount, v.LessonsRenewMonthly, v.DurationMonths, v.DurationDays);
        public ServicePackageVariant Variant() => new() { PersonalTrainingSessionCount = Pt, PerformanceClassCreditCount = Performance,
            ReformerClassCreditCount = Reformer, GroupClassCreditCount = Group, KidsClassCreditCount = Kids, LessonsRenewMonthly = Monthly, DurationMonths = Months, DurationDays = Days };
    }
    public async Task<(int? OrderId, string? Error)> CreateOrderAsync(
        string userId, int variantId, string fullName, string phone,
        string address, string district, string city,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await using var tx = dbContext.Database.IsRelational() && dbContext.Database.CurrentTransaction is null
                ? await dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken) : null;
            var result = await CreateOrderCoreAsync(userId, variantId, fullName, phone, address, district, city, cancellationToken);
            if (result.OrderId.HasValue && tx is not null) await tx.CommitAsync(cancellationToken);
            return result;
        }
        catch (Exception e) when (e is DbUpdateException || e.GetBaseException() is Npgsql.PostgresException { SqlState: "40001" or "40P01" })
        { return (null, "Paket işlemi başka bir istekle değişti. Sayfayı yenileyip tekrar deneyin."); }
    }

    private async Task<(int? OrderId, string? Error)> CreateOrderCoreAsync(
        string userId, int variantId, string fullName, string phone,
        string address, string district, string city, CancellationToken cancellationToken)
    {
        var profile = await dbContext.MemberProfiles
            .FirstOrDefaultAsync(x => x.ApplicationUserId == userId, cancellationToken);
        if (profile is null)
            return (null, "Üye profili bulunamadı.");
        await MemberCreditGuard.LockAsync(dbContext, profile.Id);

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
                (x.ServicePackage.Category == ServicePackageCategory.Membership || x.ServicePackage.Category == ServicePackageCategory.PersonalTraining ||
                 x.ServicePackage.Category == ServicePackageCategory.GroupClasses || x.ServicePackage.Category == ServicePackageCategory.KidsClub),
                cancellationToken);
        if (variant is null ||
            variant.PriceOnRequest || variant.TotalPrice <= 0 ||
            variant.DurationMonths is <= 0 || variant.DurationDays is <= 0)
            return (null, "Bu paket çevrimiçi yenilemeye uygun değil.");

        var order = new Order
        {
            OrderNumber = $"NO23-UY-{Guid.NewGuid():N}"[..32],
            MemberProfileId = profile.Id,
            ServicePackageVariantId = variant.Id,
            LessonGrantSnapshotJson = JsonSerializer.Serialize(CreditSnapshot.From(variant)),
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
        await using var tx = dbContext.Database.IsRelational() && dbContext.Database.CurrentTransaction is null
            ? await dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken) : null;
        var result = await ActivatePaidOrderCoreAsync(orderId, cancellationToken);
        if (result && tx is not null) await tx.CommitAsync(cancellationToken);
        return result;
    }

    private async Task<bool> ActivatePaidOrderCoreAsync(int orderId, CancellationToken cancellationToken)
    {
        var order = await dbContext.Orders
            .Include(x => x.ServicePackageVariant)
                .ThenInclude(x => x!.ServicePackage)
            .FirstOrDefaultAsync(x => x.Id == orderId &&
                x.Type == OrderType.MembershipRenewal &&
                x.PaymentStatus == PaymentStatus.Paid,
                cancellationToken);
        if (order?.MemberProfileId is null || order.ServicePackageVariant is null ||
            !Enum.IsDefined(order.ServicePackageVariant.ServicePackage.Category))
            return false;
        await MemberCreditGuard.LockAsync(dbContext, order.MemberProfileId.Value);

        var profile = await dbContext.MemberProfiles
            .FirstOrDefaultAsync(x => x.Id == order.MemberProfileId, cancellationToken);
        if (profile is null)
            return false;
        if (profile.LastMembershipOrderId == order.Id)
            return true;
        if (profile.MembershipEndsAtUtc > order.CreatedAtUtc)
            return false;

        var variant = order.ServicePackageVariant;
        var entitlementVariant = string.IsNullOrWhiteSpace(order.LessonGrantSnapshotJson) ? variant :
            JsonSerializer.Deserialize<CreditSnapshot>(order.LessonGrantSnapshotJson)?.Variant();
        if (entitlementVariant is null) return false;
        var startsAt = DateTime.UtcNow;
        var endsAt = MemberPackageEntitlement.CalculateEndDate(entitlementVariant, startsAt);

        profile.MembershipPackageId = variant.ServicePackage.MembershipPackageId ?? await dbContext.MembershipPackages
            .Where(x => x.Code == MembershipPackageCode.Start).Select(x => x.Id).SingleAsync(cancellationToken);
        profile.MembershipPackageOptionId = null;
        profile.ServicePackageVariantId = variant.Id;
        CategoryCreditService.Initialize(profile, entitlementVariant, "system:paid-package-activation");
        profile.MembershipStartsAtUtc = startsAt;
        profile.MembershipEndsAtUtc = endsAt;
        profile.LastMembershipOrderId = order.Id;
        profile.UpdatedAtUtc = startsAt;
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}
