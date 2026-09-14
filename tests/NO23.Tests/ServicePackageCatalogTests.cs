using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NO23.Web.Controllers;
using NO23.Web.Data;
using NO23.Web.Data.Seed;
using NO23.Web.Domain.Entities;
using NO23.Web.Domain.Enums;
using NO23.Web.ViewModels.Plans;

namespace NO23.Tests;

public class ServicePackageCatalogTests
{
    [Fact]
    public void Defaults_CoverAllPublicCategories()
    {
        foreach (var category in Enum.GetValues<ServicePackageCategory>())
            Assert.Contains(ServicePackageSeed.Defaults, x => x.Category == category);
    }

    [Fact]
    public void Defaults_HavePositivePricesAndAtLeastOneVariant()
    {
        Assert.All(ServicePackageSeed.Defaults, package =>
        {
            Assert.NotEmpty(package.Variants);
            Assert.All(package.Variants, variant =>
                Assert.True(variant.PriceOnRequest || variant.TotalPrice > 0 || variant.MonthlyPrice > 0));
        });
    }

    [Fact]
    public void Defaults_MatchPublishedPersonalTrainingAndDuetPrices()
    {
        AssertPrices("pt-flex", ("8 Ders", 14000m), ("12 Ders", 19500m));
        AssertPrices("pt-routine", ("24 Ders", 36000m), ("36 Ders", 54000m));
        AssertPrices("pt-commit", ("50 Ders", 70000m), ("70 Ders", 98000m), ("100 Ders", 130000m));
        AssertPrices("duet-pt", ("8 Ders · Kişi Başı", 10000m), ("12 Ders · Kişi Başı", 14400m),
            ("24 Ders · Kişi Başı", 27600m), ("36 Ders · Kişi Başı", 41400m),
            ("50 Ders · Kişi Başı", 55000m), ("70 Ders · Kişi Başı", 77000m),
            ("100 Ders · Kişi Başı", 100000m));
        Assert.All(ServicePackageSeed.Defaults.SelectMany(x => x.Variants), x => Assert.False(x.PriceOnRequest));
    }

    [Fact]
    public void Defaults_MatchPublishedGroupAndKidsTerms()
    {
        AssertPrices("group-reformer", ("8 Ders", 5000m), ("6 Aylık (Ayda 8 Ders)", 25000m),
            ("Yıllık (Ayda 8 Ders)", 50000m));
        AssertPrices("group-reformer-plus", ("8 Reformer + 4 Performance", 7500m),
            ("6 Aylık", 37500m), ("Yıllık", 75000m));
        AssertPrices("group-performance", ("8 Ders", 6000m), ("8 Ders · 6 Aylık", 30000m),
            ("8 Ders · Yıllık", 60000m), ("12 Ders", 8400m), ("12 Ders · 6 Aylık", 42000m),
            ("12 Ders · Yıllık", 84000m), ("24 Ders · 3 Aylık", 15600m));
        AssertPrices("kids-club", ("8 Ders", 5000m), ("8 Ders · 6 Aylık", 25000m),
            ("8 Ders · Yıllık", 50000m));

        var reformerPlus = ServicePackageSeed.Defaults.Single(x => x.Slug == "group-reformer-plus");
        Assert.All(reformerPlus.Variants, x =>
        {
            Assert.Equal(8, x.ReformerClassCreditCount);
            Assert.Equal(4, x.PerformanceClassCreditCount);
        });
        var performance24 = ServicePackageSeed.Defaults.Single(x => x.Slug == "group-performance")
            .Variants.Single(x => x.Name == "24 Ders · 3 Aylık");
        Assert.Equal(3, performance24.DurationMonths);
        Assert.False(performance24.LessonsRenewMonthly);
        Assert.Contains(ServicePackageSeed.Defaults.Single(x => x.Slug == "kids-club").Features,
            x => x.Text.Contains("%25 kardeş indirimi"));
    }

    [Fact]
    public void Defaults_MatchPublishedMembershipDiscounts()
    {
        var hybrid = ServicePackageSeed.Defaults.Single(x => x.Slug == "hybrid");
        Assert.Equal((5, 0, 0), (hybrid.KitchenDiscountPercent, hybrid.CoffeeDiscountPercent, hybrid.ShopDiscountPercent));
        var pro = ServicePackageSeed.Defaults.Single(x => x.Slug == "pro-membership");
        Assert.Equal((10, 10, 5), (pro.KitchenDiscountPercent, pro.CoffeeDiscountPercent, pro.ShopDiscountPercent));
        var black = ServicePackageSeed.Defaults.Single(x => x.Slug == "black");
        Assert.Equal((15, 15, 5), (black.KitchenDiscountPercent, black.CoffeeDiscountPercent, black.ShopDiscountPercent));
        Assert.Contains(black.Features, x => x.Text == "Geniş Performance erişimi");
    }

    [Fact]
    public async Task Catalog_ReturnsOnlyRequestedCategoryAndActiveRecords()
    {
        await using var db = CreateDbContext();
        db.ServicePackages.AddRange(
            Package(ServicePackageCategory.PersonalTraining, "pt-active", true),
            Package(ServicePackageCategory.PersonalTraining, "pt-passive", false),
            Package(ServicePackageCategory.KidsClub, "kids-active", true));
        await db.SaveChangesAsync();

        var result = await new PlansController(db).Index("personal-training");

        var view = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<ServicePackageCatalogViewModel>(view.Model);
        Assert.Equal(ServicePackageCategory.PersonalTraining, model.Category);
        Assert.Equal("pt-active", Assert.Single(model.Packages).Slug);
    }

    private static ServicePackage Package(ServicePackageCategory category, string slug, bool active)
    {
        var package = new ServicePackage
        { Category=category,Slug=slug,Name=slug,Subtitle="Test",Description="Test",IsActive=active,DisplayOrder=1 };
        package.Variants.Add(new ServicePackageVariant
        { Name="Standart",BillingType=ServicePackageBillingType.OneTime,TotalPrice=1000,IsActive=true,DisplayOrder=1 });
        return package;
    }

    private static void AssertPrices(string slug, params (string Name, decimal Price)[] expected)
    {
        var package = ServicePackageSeed.Defaults.Single(x => x.Slug == slug);
        Assert.Equal(expected.Length, package.Variants.Count);
        foreach (var item in expected)
            Assert.Equal(item.Price, package.Variants.Single(x => x.Name == item.Name).TotalPrice);
    }

    private static ApplicationDbContext CreateDbContext() => new(
        new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
}
