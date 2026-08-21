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
                Assert.True(variant.TotalPrice > 0 || variant.MonthlyPrice > 0));
        });
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

    private static ApplicationDbContext CreateDbContext() => new(
        new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
}
