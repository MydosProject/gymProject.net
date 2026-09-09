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
        Assert.Equal(other.Variants.Select(x => x.Price), card.Variants.Select(x => x.Price));
        Assert.Equal(3, card.Variants.Count);
        Assert.DoesNotContain(card.Variants, x => x.Rights.Contains("Salon"));
    }

    [Fact]
    public async Task Savings_UseSameTermAndPtUnitPrice_AndQuotesNeverShowZero()
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
        var quote = routine.Variants[1];
        Assert.Equal("Fiyat için bilgi al", quote.Price);
        Assert.Null(quote.Savings);
        Assert.Null(quote.UnitPrice);
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
        Assert.Equal(12, annual.ReformerClassCreditCount);
        Assert.Equal(0, annual.PerformanceClassCreditCount);
        var presentation = ServicePackageCatalogService.Present(annual);
        Assert.Contains("toplam 14 ay", presentation.CommitmentNote);
        Assert.Contains("Her ay: 12 Reformer", presentation.Rights);
        var controller = new PlansController(db) { ControllerContext = new ControllerContext { HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext() } };
        var application = Assert.IsType<PlanApplicationPageViewModel>(Assert.IsType<ViewResult>(await controller.Apply(plus.Slug, annual.Id)).Model);
        Assert.Equal(presentation.CommitmentNote, application.CommitmentNote);
        Assert.Contains("Fiyat için bilgi al", application.VariantPrice);
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

    private static ApplicationDbContext Db() => new(new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
}
