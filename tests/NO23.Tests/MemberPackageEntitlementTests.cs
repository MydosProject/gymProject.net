using NO23.Web.Domain.Entities;
using NO23.Web.Data.Seed;
using NO23.Web.Services;

namespace NO23.Tests;

public class MemberPackageEntitlementTests
{
    [Theory]
    [InlineData("group-reformer", 6, 48)]
    [InlineData("group-reformer", 12, 96)]
    [InlineData("group-reformer-plus", 6, 72)]
    [InlineData("group-reformer-plus", 12, 144)]
    [InlineData("group-performance", 6, 48)]
    [InlineData("group-performance", 12, 96)]
    [InlineData("kids-club", 6, 48)]
    [InlineData("kids-club", 12, 96)]
    public void GiftMonths_AreIncludedInPublishedTotalDurationAndCredits(string slug, int months, int credits)
    {
        var package = ServicePackageSeed.Defaults.Single(x => x.Slug == slug);
        var variant = package.Variants.First(x => x.DurationMonths == months);
        var starts = new DateTime(2026, 10, 14, 9, 0, 0, DateTimeKind.Utc);

        Assert.Equal(months == 6 ? 1 : 2, variant.BonusMonths);
        Assert.Equal(starts.AddMonths(months), MemberPackageEntitlement.CalculateEndDate(variant, starts));
        Assert.Equal($"{months} ay", MemberPackageEntitlement.DurationLabel(variant));
        Assert.Equal(credits, MemberPackageEntitlement.CalculateInitialCredits(variant));
        Assert.Contains($"Hediye ay dahil toplam kullanım süresi {months} ay.",
            ServicePackageCatalogService.Present(variant).CommitmentNote);
    }

    [Fact]
    public void SixMonthReformer_PreservesThePublishedPriceUnitPriceAndSavings()
    {
        var package = ServicePackageSeed.Defaults.Single(x => x.Slug == "group-reformer");
        var variant = package.Variants.Single(x => x.DurationMonths == 6);
        var shown = ServicePackageCatalogService.Present(variant, package.Variants);

        Assert.Equal("25.000,00 ₺", shown.Price);
        Assert.Equal("520,83 ₺ / ders", shown.UnitPrice);
        Assert.Equal("5.000,00 ₺ tasarruf (%16,7)", shown.Savings);
        Assert.Equal(48, MemberPackageEntitlement.CalculateInitialCredits(variant));
    }

    [Fact]
    public void DayBasedPackageAndFlexibleMonth_KeepTheirExistingDurationRules()
    {
        var starts = new DateTime(2026, 1, 31, 9, 0, 0, DateTimeKind.Utc);
        var daily = new ServicePackageVariant { DurationDays = 30 };
        var flexible = new ServicePackageVariant();

        Assert.Equal(starts.AddDays(30), MemberPackageEntitlement.CalculateEndDate(daily, starts));
        Assert.Equal("30 gün", MemberPackageEntitlement.DurationLabel(daily));
        Assert.Equal(new DateTime(2026, 2, 28, 9, 0, 0, DateTimeKind.Utc), MemberPackageEntitlement.CalculateEndDate(flexible, starts));
        Assert.Equal("1 ay", MemberPackageEntitlement.DurationLabel(flexible));
    }

    [Fact]
    public void CalculateInitialCredits_MultipliesMonthlyRightsByDuration()
    {
        var variant = new ServicePackageVariant
        {
            DurationMonths = 6,
            LessonsRenewMonthly = true,
            ReformerClassCreditCount = 8,
            PerformanceClassCreditCount = 4
        };

        Assert.Equal(72, MemberPackageEntitlement.CalculateInitialCredits(variant));
    }

    [Fact]
    public void CalculateInitialCredits_DoesNotMultiplyFixedTermPackage()
    {
        var variant = new ServicePackageVariant
        {
            DurationMonths = 3,
            LessonsRenewMonthly = false,
            KidsClassCreditCount = 24
        };

        Assert.Equal(24, MemberPackageEntitlement.CalculateInitialCredits(variant));
    }
}
