using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NO23.Web.Controllers;
using NO23.Web.Data;
using NO23.Web.Data.Seed;
using NO23.Web.Domain.Entities;
using NO23.Web.Domain.Enums;
using NO23.Web.Services;
using NO23.Web.ViewModels.Home;
using NO23.Web.ViewModels.Plans;

namespace NO23.Tests;

public class PublicCatalogRegressionTests
{
    [Fact]
    public async Task HomeAndCatalog_UseSameActivePackageAndCurrentAdminContent()
    {
        await using var db = Db();
        db.MembershipPackages.Add(new MembershipPackage { Name = "Eski PLUS", Audience = "Eski", Description = "Eski" });
        var package = ServicePackageSeed.Defaults.First();
        package.Name = "Güncel HYBRID";
        package.Description = "Yönetim panelinden değiştirildi";
        package.Variants.First().MonthlyPrice = 15000;
        package.Variants.Last().IsActive = false;
        db.ServicePackages.Add(package);
        var inactive = ServicePackageSeed.Defaults[1];
        inactive.IsActive = false;
        db.ServicePackages.Add(inactive);
        await db.SaveChangesAsync();
        var home = Assert.IsType<HomeIndexViewModel>(Assert.IsType<ViewResult>(await new HomeController(db).Index()).Model);
        var catalog = Assert.IsType<ServicePackageCatalogViewModel>(Assert.IsType<ViewResult>(await new PlansController(db).Index("membership")).Model);
        var card = Assert.Single(home.MembershipPackages);
        var other = Assert.Single(catalog.Packages);
        Assert.Equal("Güncel HYBRID", card.Name);
        Assert.Equal(other.Description, card.Description);
        Assert.Equal(other.Features, card.Features);
        Assert.Equal(other.Highlights, card.Highlights);
        Assert.Equal(other.Variants.Select(x => x.Price), card.Variants.Select(x => x.Price));
        Assert.Equal(3, card.Variants.Count);
        Assert.DoesNotContain(card.Variants, x => x.Rights.Contains("Salon"));
    }

    [Fact]
    public async Task Savings_UseSameTermAndPtUnitPrice_WithPublishedPrices()
    {
        await using var db = Db();
        db.ServicePackages.AddRange(ServicePackageSeed.Defaults);
        await db.SaveChangesAsync();
        var service = new ServicePackageCatalogService(db);
        var membership = (await service.LoadAsync(ServicePackageCategory.Membership)).First();
        var annual = membership.Variants.Single(x => x.Name == "12 Aylık");
        Assert.Contains("18.000,00", annual.Savings);
        Assert.Contains("138.000,00", annual.CommitmentNote);
        var pt = await service.LoadAsync(ServicePackageCategory.PersonalTraining);
        var routine = pt.Single(x => x.Slug == "pt-routine");
        Assert.Contains("6.000,00", routine.Variants[0].Savings);
        Assert.Contains("1.500,00", routine.Variants[0].UnitPrice);
        var thirtySixLessons = routine.Variants[1];
        Assert.Contains("54.000,00", thirtySixLessons.Price);
        Assert.Contains("9.000,00", thirtySixLessons.Savings);
        Assert.Contains("1.500,00", thirtySixLessons.UnitPrice);
    }

    [Fact]
    public void Savings_DoNotCompareDifferentLessonRightsOrValidity()
    {
        var reference = new ServicePackageVariant { BillingType = ServicePackageBillingType.MonthlySubscription, MonthlyPrice = 100, PersonalTrainingSessionCount = 2 };
        var longer = new ServicePackageVariant { BillingType = reference.BillingType, MonthlyPrice = 50, DurationMonths = 12, PersonalTrainingSessionCount = 1 };
        Assert.Null(ServicePackageCatalogService.Present(longer, [reference]).Savings);
        reference.BillingType = ServicePackageBillingType.OneTime;
        reference.TotalPrice = 200;
        reference.DurationDays = 30;
        longer.BillingType = ServicePackageBillingType.OneTime;
        longer.PersonalTrainingSessionCount = 12;
        longer.TotalPrice = 500;
        longer.DurationDays = 5;
        Assert.Null(ServicePackageCatalogService.Present(longer, ptReference: reference).Savings);
    }

    [Fact]
    public async Task GiftsAndLessonOptions_AppearInCatalogAndApplication()
    {
        await using var db = Db();
        var plus = ServicePackageSeed.Defaults.Single(x => x.Slug == "group-reformer-plus");
        db.ServicePackages.Add(plus);
        await db.SaveChangesAsync();
        var annual = plus.Variants.Single(x => x.DurationMonths == 12);
        Assert.Equal(8, annual.ReformerClassCreditCount);
        Assert.Equal(4, annual.PerformanceClassCreditCount);
        var presentation = ServicePackageCatalogService.Present(annual);
        Assert.Contains("12 aylık paketin 2 ayı hediye", presentation.CommitmentNote);
        Assert.Contains("Her ay: 8 Reformer", presentation.Rights);
        Assert.Contains("4 Performance", presentation.Rights);
        var controller = new PlansController(db) { ControllerContext = new ControllerContext { HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext() } };
        var application = Assert.IsType<PlanApplicationPageViewModel>(Assert.IsType<ViewResult>(await controller.Apply(plus.Slug, annual.Id)).Model);
        Assert.Equal(presentation.CommitmentNote, application.CommitmentNote);
        Assert.Contains("75.000,00", application.VariantPrice);
    }

    [Fact]
    public async Task LargerGroupBundles_ShowPerLessonSavings()
    {
        await using var db = Db();
        db.ServicePackages.Add(ServicePackageSeed.Defaults.Single(x => x.Slug == "group-performance"));
        await db.SaveChangesAsync();

        var package = (await new ServicePackageCatalogService(db).LoadAsync(ServicePackageCategory.GroupClasses)).Single();
        var twelve = package.Variants.Single(x => x.Name == "12 Ders");

        Assert.Contains("600,00", twelve.Savings);
        Assert.Contains("8 Ders birim fiyatına göre", twelve.ComparisonNote);
    }

    [Fact]
    public async Task LongTermGroupPackages_ShowDiscountAgainstTheMonthlyPackage()
    {
        await using var db = Db();
        db.ServicePackages.Add(ServicePackageSeed.Defaults.Single(x => x.Slug == "group-reformer"));
        await db.SaveChangesAsync();

        var package = Assert.Single(await new ServicePackageCatalogService(db).LoadAsync(ServicePackageCategory.GroupClasses));
        var sixMonths = package.Variants.Single(x => x.Name == "6 Aylık (Ayda 8 Ders)");
        var annual = package.Variants.Single(x => x.Name == "Yıllık (Ayda 8 Ders)");

        Assert.Contains("5.000,00", sixMonths.Savings);
        Assert.Contains("520,83", sixMonths.UnitPrice);
        Assert.Contains("10.000,00", annual.Savings);
        Assert.Contains("520,83", annual.UnitPrice);
        Assert.Contains("Hediye ay dahil toplam kullanım süresi 12 ay", annual.CommitmentNote);
    }

    [Fact]
    public async Task MembershipCards_SeparateCommercialHighlightsAndExplainMonthlyCollection()
    {
        await using var db = Db();
        db.ServicePackages.Add(ServicePackageSeed.Defaults.Single(x => x.Slug == "pro-membership"));
        await db.SaveChangesAsync();

        var package = Assert.Single(await new ServicePackageCatalogService(db).LoadAsync(ServicePackageCategory.Membership));
        Assert.Contains(package.Highlights, x => x.Contains("Kitchen aboneliğinde %10 indirim"));
        Assert.Contains(package.Highlights, x => x.Contains("Coffee’de %10 indirim"));
        Assert.Contains(package.Highlights, x => x.Contains("Shop ürünlerinde %5 indirim"));
        Assert.DoesNotContain(package.Features, x => x.Contains("indirim", StringComparison.OrdinalIgnoreCase));
        Assert.All(package.Variants, x =>
        {
            Assert.Contains("her ay karttan otomatik tahsil", x.BillingNote, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("Başvuru tek başına ödeme oluşturmaz", x.BillingNote);
        });
    }

    private static ApplicationDbContext Db() => new(new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
}
