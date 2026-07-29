using NO23.Web.Data.Seed;
using NO23.Web.Domain.Entities;
using NO23.Web.Domain.Enums;

namespace NO23.Tests;

public class KitchenSubscriptionPackageSeedTests
{
    [Fact]
    public void Defaults_DefineOneActivePackageForEachPlan()
    {
        var packages = KitchenSubscriptionPackageSeed.Defaults;

        Assert.Equal(Enum.GetValues<KitchenSubscriptionPlan>().Length, packages.Count);
        Assert.Equal(
            packages.Count,
            packages.Select(package => package.Plan).Distinct().Count());
        Assert.All(packages, package =>
        {
            Assert.True(package.IsActive);
            Assert.True(package.Days > 0);
            Assert.True(package.UnitPrice > 0);
            Assert.False(string.IsNullOrWhiteSpace(package.Name));
            Assert.False(string.IsNullOrWhiteSpace(package.Description));
        });
    }

    [Theory]
    [InlineData(KitchenSubscriptionPlan.FiveDays, 5, 4250)]
    [InlineData(KitchenSubscriptionPlan.TenDays, 10, 7900)]
    [InlineData(KitchenSubscriptionPlan.TwentyDays, 20, 14500)]
    [InlineData(KitchenSubscriptionPlan.Monthly, 30, 19900)]
    public void Defaults_MatchPublishedPackageDurationAndPrice(
        KitchenSubscriptionPlan plan,
        int expectedDays,
        int expectedPrice)
    {
        var package = Assert.Single(
            KitchenSubscriptionPackageSeed.Defaults,
            item => item.Plan == plan);

        Assert.Equal(expectedDays, package.Days);
        Assert.Equal((decimal)expectedPrice, package.UnitPrice);
    }

    [Fact]
    public void KitchenSubscription_KeepsPackagePriceSnapshot()
    {
        var package = new KitchenSubscriptionPackage
        {
            Id = 1,
            Plan = KitchenSubscriptionPlan.FiveDays,
            Name = "5 Günlük Kitchen Paketi",
            Days = 5,
            UnitPrice = 4250
        };

        var subscription = new KitchenSubscription
        {
            KitchenSubscriptionPackageId = package.Id,
            Plan = package.Plan,
            PackageNameSnapshot = package.Name,
            PackageDaysSnapshot = package.Days,
            PackagePriceSnapshot = package.UnitPrice
        };

        package.UnitPrice = 5000;

        Assert.Equal(4250, subscription.PackagePriceSnapshot);
    }
}
