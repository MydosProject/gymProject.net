using System.Globalization;
using Microsoft.EntityFrameworkCore;
using NO23.Web.Data;
using NO23.Web.Domain.Entities;
using NO23.Web.Domain.Enums;
using NO23.Web.ViewModels.Plans;

namespace NO23.Web.Services;

public class ServicePackageCatalogService(ApplicationDbContext dbContext)
{
    private static readonly CultureInfo Turkish = CultureInfo.GetCultureInfo("tr-TR");

    public async Task<IReadOnlyList<ServicePackageCardViewModel>> LoadAsync(ServicePackageCategory category)
    {
        var packages = await dbContext.ServicePackages.AsNoTracking()
            .Include(x => x.Features).Include(x => x.Variants).Include(x => x.MembershipPackage)
            .Where(x => x.Category == category && x.IsActive)
            .OrderBy(x => x.DisplayOrder).ThenBy(x => x.Name)
            .ToListAsync();
        var ptReference = packages.Where(x => x.Slug == "pt-flex")
            .SelectMany(x => x.Variants).Where(IsPricedPt)
            .OrderBy(x => x.PersonalTrainingSessionCount).FirstOrDefault();

        return packages.Select(package => new ServicePackageCardViewModel
        {
            CategoryTitle = CategoryTitle(category), Slug = package.Slug,
            Name = package.Name, Subtitle = package.Subtitle, Description = package.Description,
            IsFeatured = package.IsFeatured,
            MembershipCode = package.MembershipPackage?.Code.ToString().ToUpperInvariant(),
            Features = package.Features.OrderBy(x => x.DisplayOrder).Select(x => x.Text)
                .Concat(BenefitLabels(package)).ToList(),
            Variants = package.Variants.Where(x => x.IsActive).OrderBy(x => x.DisplayOrder)
                .Select(x => Present(x, package.Variants, ptReference)).ToList()
        }).ToList();
    }

    public static ServicePackageVariantCardViewModel Present(ServicePackageVariant variant,
        IEnumerable<ServicePackageVariant>? siblings = null, ServicePackageVariant? ptReference = null)
    {
        var monthly = variant.BillingType == ServicePackageBillingType.MonthlySubscription;
        string? unitPrice = null, savings = null, comparison = null;
        if (!variant.PriceOnRequest)
        {
            decimal? referenceTotal = null;
            if (monthly && variant.DurationMonths is > 0 && variant.MonthlyPrice is > 0)
            {
                var reference = siblings?.Where(x => x.IsActive && !x.PriceOnRequest &&
                    x.BillingType == ServicePackageBillingType.MonthlySubscription &&
                    x.DurationMonths == null && x.MonthlyPrice is > 0 && SameRights(x, variant))
                    .OrderBy(x => x.MonthlyPrice).FirstOrDefault();
                if (reference is not null)
                {
                    referenceTotal = reference.MonthlyPrice * variant.DurationMonths;
                    comparison = $"{reference.Name} seçeneğinin {variant.DurationMonths} aylık bedeline göre";
                }
            }
            else if (!monthly && IsPricedPt(variant))
            {
                unitPrice = $"{Money(variant.TotalPrice / variant.PersonalTrainingSessionCount)} / ders";
                // Compare only equal validity periods and pure PT lesson bundles.
                if (ptReference is not null && IsPricedPt(ptReference) &&
                    variant.PersonalTrainingSessionCount > ptReference.PersonalTrainingSessionCount &&
                    variant.DurationDays == ptReference.DurationDays && variant.DurationMonths == ptReference.DurationMonths)
                {
                    referenceTotal = ptReference.TotalPrice / ptReference.PersonalTrainingSessionCount * variant.PersonalTrainingSessionCount;
                    comparison = $"FLEX {ptReference.PersonalTrainingSessionCount} dersin birim fiyatına göre";
                }
            }
            else if (!monthly && LessonCount(variant) > 0)
            {
                unitPrice = $"{Money(variant.TotalPrice / LessonCount(variant))} / ders";
                var reference = siblings?.Where(x => x.IsActive && !x.PriceOnRequest &&
                    x.BillingType == ServicePackageBillingType.OneTime &&
                    x.DurationMonths == variant.DurationMonths && x.DurationDays == variant.DurationDays &&
                    LessonCount(x) > 0 && LessonCount(x) < LessonCount(variant))
                    .OrderBy(x => LessonCount(x)).FirstOrDefault();
                if (reference is not null)
                {
                    referenceTotal = reference.TotalPrice / LessonCount(reference) * LessonCount(variant);
                    comparison = $"{reference.Name} birim fiyatına göre";
                }
            }
            var total = monthly ? (variant.MonthlyPrice ?? 0) * (variant.DurationMonths ?? 1) : variant.TotalPrice;
            if (referenceTotal > total && total > 0)
                savings = $"{Money(referenceTotal.Value - total)} tasarruf (%{((referenceTotal.Value - total) / referenceTotal.Value * 100).ToString("0.#", Turkish)})";
            else
                comparison = null;
        }

        return new ServicePackageVariantCardViewModel
        {
            Id = variant.Id, Name = variant.Name, Rights = Rights(variant), IsRecommended = variant.IsRecommended,
            Price = variant.PriceOnRequest ? "Fiyat için bilgi al" : Money(monthly ? variant.MonthlyPrice ?? 0 : variant.TotalPrice),
            PriceNote = variant.PriceOnRequest ? "Başvuru sonrası teklif" : monthly ? "aylık" : "tek seferlik",
            UnitPrice = unitPrice, Savings = savings, ComparisonNote = comparison,
            BillingNote = monthly ? "Üyelik bedeli her ay otomatik tahsil edilir. Aktivasyon ve ödeme koşulları başvuru sonrasında netleştirilir." : null,
            CommitmentNote = variant.BonusMonths > 0 && variant.DurationMonths is > 0
                ? $"{variant.DurationMonths} ay + {variant.BonusMonths} ay hediye = toplam {variant.DurationMonths + variant.BonusMonths} ay. Hediye aylarda aynı ders hakları devam eder."
                : monthly && variant.DurationMonths is > 0 && !variant.PriceOnRequest
                ? $"{variant.DurationMonths} ay boyunca toplam {Money((variant.MonthlyPrice ?? 0) * variant.DurationMonths.Value)}"
                : variant.DurationDays is > 0 ? $"Kullanım süresi: {variant.DurationDays} gün"
                : variant.DurationMonths is > 0 ? $"Kullanım süresi: {variant.DurationMonths} ay" : null
        };
    }

    public static string Rights(ServicePackageVariant x) => (x.LessonsRenewMonthly || x.BillingType == ServicePackageBillingType.MonthlySubscription ? "Her ay: " : "") + string.Join(" · ", new[]
    {
        x.PersonalTrainingSessionCount > 0 ? $"{x.PersonalTrainingSessionCount} PT dersi" : null,
        x.ReformerClassCreditCount > 0 ? $"{x.ReformerClassCreditCount} Reformer dersi" : null,
        x.PerformanceClassCreditCount > 0 ? $"{x.PerformanceClassCreditCount} Performance dersi" : null,
        x.GroupClassCreditCount > 0 ? $"{x.GroupClassCreditCount} grup dersi" : null,
        x.KidsClassCreditCount > 0 ? $"{x.KidsClassCreditCount} ders" : null
    }.Where(x => x is not null));

    public static string CategoryTitle(ServicePackageCategory category) => category switch
    {
        ServicePackageCategory.Membership => "Üyelik", ServicePackageCategory.PersonalTraining => "Personal Training",
        ServicePackageCategory.GroupClasses => "Grup Dersleri", ServicePackageCategory.KidsClub => "Kids Club", _ => "Paketler"
    };

    private static string Money(decimal amount) => $"{amount.ToString("N2", Turkish)} ₺";
    private static IEnumerable<string> BenefitLabels(ServicePackage package)
    {
        if (package.KitchenDiscountPercent > 0) yield return $"Kitchen aboneliğinde %{package.KitchenDiscountPercent} indirim";
        if (package.CoffeeDiscountPercent > 0) yield return $"NO:23 Kitchen & Coffee’de %{package.CoffeeDiscountPercent} indirim";
        if (package.ShopDiscountPercent > 0) yield return $"NO:23 Shop ürünlerinde %{package.ShopDiscountPercent} indirim";
        if (package.IncludesRecoveryRoom) yield return "Recovery Room: buz odasını ücretsiz kullanma";
    }
    private static int LessonCount(ServicePackageVariant x) => x.PersonalTrainingSessionCount + x.ReformerClassCreditCount +
        x.PerformanceClassCreditCount + x.GroupClassCreditCount + x.KidsClassCreditCount;
    private static bool IsPricedPt(ServicePackageVariant x) => x.IsActive && !x.PriceOnRequest &&
        x.BillingType == ServicePackageBillingType.OneTime && x.TotalPrice > 0 &&
        x.PersonalTrainingSessionCount > 0 && LessonCount(x) == x.PersonalTrainingSessionCount;
    private static bool SameRights(ServicePackageVariant a, ServicePackageVariant b) =>
        a.PersonalTrainingSessionCount == b.PersonalTrainingSessionCount && a.ReformerClassCreditCount == b.ReformerClassCreditCount &&
        a.PerformanceClassCreditCount == b.PerformanceClassCreditCount && a.GroupClassCreditCount == b.GroupClassCreditCount &&
        a.KidsClassCreditCount == b.KidsClassCreditCount;
}
