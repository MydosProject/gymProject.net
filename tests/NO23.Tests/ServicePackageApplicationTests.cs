using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using NO23.Web.Controllers;
using NO23.Web.Data;
using NO23.Web.Domain.Entities;
using NO23.Web.Domain.Enums;
using NO23.Web.ViewModels.Plans;

namespace NO23.Tests;

public class ServicePackageApplicationTests
{
    [Fact]
    public async Task Apply_Get_CarriesSelectedKidsVariantToForm()
    {
        await using var dbContext = CreateDbContext();
        var (_, variant) = await SeedKidsPackageAsync(dbContext);
        var controller = CreateController(dbContext);

        var result = await controller.Apply("kids-club", variant.Id);

        var view = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<PlanApplicationPageViewModel>(view.Model);
        Assert.Equal("8 Ders", model.VariantName);
        Assert.Equal(variant.Id, model.Input.ServicePackageVariantId);
        Assert.Equal("Kids Club", model.PackageCategory);
    }

    [Fact]
    public async Task Apply_Post_PersistsApplicationAndRedirectsToConfirmation()
    {
        await using var dbContext = CreateDbContext();
        var (package, variant) = await SeedKidsPackageAsync(dbContext);
        var controller = CreateController(dbContext);
        var input = new PlanApplicationInputViewModel
        {
            ServicePackageId = package.Id,
            ServicePackageVariantId = variant.Id,
            FullName = "Test Kullanıcı",
            Email = "test@example.com",
            PhoneNumber = "05555555555",
            Notes = "Hafta sonu uygunum."
        };

        var result = await controller.Apply(input);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(PlansController.ApplicationReceived),
            redirect.ActionName);

        var application = await dbContext.ServicePackageApplications
            .SingleAsync();
        Assert.Equal(package.Id, application.ServicePackageId);
        Assert.Equal(variant.Id, application.ServicePackageVariantId);
        Assert.Equal("Test Kullanıcı", application.FullName);
        Assert.Equal(ServicePackageApplicationStatus.Pending,
            application.Status);
    }

    [Fact]
    public async Task Apply_Post_DoesNotDuplicateImmediateRepeat()
    {
        await using var dbContext = CreateDbContext();
        var (package, variant) = await SeedKidsPackageAsync(dbContext);
        var controller = CreateController(dbContext);
        var input = new PlanApplicationInputViewModel
        {
            ServicePackageId = package.Id,
            ServicePackageVariantId = variant.Id,
            FullName = "Test Kullanıcı",
            Email = "test@example.com",
            PhoneNumber = "05555555555"
        };

        await controller.Apply(input);
        await controller.Apply(input);

        Assert.Equal(1, await dbContext.ServicePackageApplications.CountAsync());
    }

    [Fact]
    public async Task Apply_Post_UsesGeneratedFamilyCodeForSecondChildDiscount()
    {
        await using var dbContext = CreateDbContext();
        var (package, variant) = await SeedKidsPackageAsync(dbContext);
        var controller = CreateController(dbContext);

        await controller.Apply(new PlanApplicationInputViewModel
        {
            ServicePackageId = package.Id,
            ServicePackageVariantId = variant.Id,
            FullName = "Birinci Çocuk",
            Email = "parent-one@example.com",
            PhoneNumber = "05555555555"
        });
        var familyCode = (await dbContext.ServicePackageApplications.SingleAsync()).FamilyCode;

        await controller.Apply(new PlanApplicationInputViewModel
        {
            ServicePackageId = package.Id,
            ServicePackageVariantId = variant.Id,
            FullName = "İkinci Çocuk",
            Email = "parent-two@example.com",
            PhoneNumber = "05555555555",
            FamilyCode = familyCode
        });

        var applications = await dbContext.ServicePackageApplications
            .OrderBy(item => item.Id)
            .ToListAsync();
        Assert.Equal(2, applications.Count);
        Assert.StartsWith("KIDS-", familyCode);
        Assert.Equal(familyCode, applications[1].FamilyCode);
        Assert.Equal(25, applications[1].SiblingDiscountPercent);
    }

    private static PlansController CreateController(
        ApplicationDbContext dbContext)
    {
        var httpContext = new DefaultHttpContext();
        var controller = new PlansController(dbContext)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            },
            TempData = new TempDataDictionary(
                httpContext,
                new MemoryTempDataProvider())
        };

        return controller;
    }

    private static async Task<(ServicePackage Package,
        ServicePackageVariant Variant)> SeedKidsPackageAsync(
        ApplicationDbContext dbContext)
    {
        var package = new ServicePackage
        {
            Category = ServicePackageCategory.KidsClub,
            Slug = "kids-club",
            Name = "Kids Club",
            Subtitle = "Başlat",
            Description = "Test paketi",
            IsActive = true
        };
        var variant = new ServicePackageVariant
        {
            ServicePackage = package,
            Name = "8 Ders",
            BillingType = ServicePackageBillingType.OneTime,
            TotalPrice = 5000,
            KidsClassCreditCount = 8,
            IsActive = true
        };
        package.Variants.Add(variant);
        dbContext.ServicePackages.Add(package);
        await dbContext.SaveChangesAsync();
        return (package, variant);
    }

    private static ApplicationDbContext CreateDbContext() => new(
        new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private sealed class MemoryTempDataProvider : ITempDataProvider
    {
        private Dictionary<string, object> values = [];

        public IDictionary<string, object> LoadTempData(
            HttpContext context) => values;

        public void SaveTempData(
            HttpContext context,
            IDictionary<string, object> values) =>
            this.values = new Dictionary<string, object>(values);
    }
}
