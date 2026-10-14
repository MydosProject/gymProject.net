using Microsoft.EntityFrameworkCore;
using NO23.Web.Controllers;
using NO23.Web.Data;
using NO23.Web.Data.Seed;
using NO23.Web.Domain.Entities;
using NO23.Web.Domain.Enums;
using NO23.Web.Services;
using NO23.Web.ViewModels.Plans;
using Microsoft.AspNetCore.Mvc;

namespace NO23.Tests;

public class GroupPackageSelectorTests
{
    [Fact]
    public async Task Performance_GroupsRightsAndTermsWithoutDependingOnNamesAndKeepsSpecialOption()
    {
        await using var db = Db();
        var package = ServicePackageSeed.Defaults.Single(x => x.Slug == "group-performance");
        foreach (var variant in package.Variants) variant.Name = $"Yönetici adı {variant.DisplayOrder}";
        db.ServicePackages.Add(package);
        await db.SaveChangesAsync();

        var card = Assert.Single(await new ServicePackageCatalogService(db).LoadAsync(ServicePackageCategory.GroupClasses));
        Assert.Equal(new[] { "8 ders", "12 ders" }, card.GroupLessonOptions.Select(x => x.Label));
        Assert.All(card.GroupLessonOptions, option => Assert.Equal(new int?[] { 1, 6, 12 }, option.Variants.Select(x => x.DurationMonths)));
        Assert.Equal(3, Assert.Single(card.OtherGroupVariants).DurationMonths);
        Assert.Equal(card.Variants.Select(x => x.Id).Order(), card.GroupLessonOptions.SelectMany(x => x.Variants)
            .Concat(card.OtherGroupVariants).Select(x => x.Id).Order());

        foreach (var variant in card.GroupLessonOptions.SelectMany(x => x.Variants).Concat(card.OtherGroupVariants))
        {
            var controller = new PlansController(db) { ControllerContext = new() { HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext() } };
            var application = Assert.IsType<PlanApplicationPageViewModel>(Assert.IsType<ViewResult>(await controller.Apply(package.Slug, variant.Id)).Model);
            Assert.Equal(variant.Id, application.Input.ServicePackageVariantId);
            Assert.Equal($"{variant.Price} · {variant.PriceNote}", application.VariantPrice);
            Assert.Equal(variant.Rights, application.VariantRights);
        }
    }

    [Fact]
    public async Task ReformerPlus_KeepsMixedLessonRightsAndGiftMonthsInOneFamily()
    {
        await using var db = Db();
        db.ServicePackages.Add(ServicePackageSeed.Defaults.Single(x => x.Slug == "group-reformer-plus"));
        await db.SaveChangesAsync();
        var card = Assert.Single(await new ServicePackageCatalogService(db).LoadAsync(ServicePackageCategory.GroupClasses));
        var family = Assert.Single(card.GroupLessonOptions);
        Assert.Equal("12 ders", family.Label);
        Assert.All(family.Variants, variant => { Assert.Contains("8 Reformer", variant.Rights); Assert.Contains("4 Performance", variant.Rights); });
        Assert.Equal(new[] { 0, 1, 2 }, family.Variants.Select(x => x.BonusMonths));
        Assert.Empty(card.OtherGroupVariants);
    }

    [Fact]
    public async Task MissingOrAmbiguousTerms_NeverCreateUnavailableCombinationsOrDiscardVariants()
    {
        await using var db = Db();
        var package = ServicePackageSeed.Defaults.Single(x => x.Slug == "group-performance");
        package.Variants.Single(x => x.PerformanceClassCreditCount == 8 && x.DurationMonths == 6).IsActive = false;
        package.Variants.Add(new ServicePackageVariant { Name = "Özel yıllık 12 ders", DurationMonths = 12,
            BillingType = ServicePackageBillingType.OneTime, LessonsRenewMonthly = true,
            PerformanceClassCreditCount = 12, TotalPrice = 85000, DisplayOrder = 20 });
        db.ServicePackages.Add(package);
        await db.SaveChangesAsync();
        var card = Assert.Single(await new ServicePackageCatalogService(db).LoadAsync(ServicePackageCategory.GroupClasses));
        Assert.Equal(new int?[] { 1, 12 }, card.GroupLessonOptions.Single(x => x.Label == "8 ders").Variants.Select(x => x.DurationMonths));
        Assert.Equal(new int?[] { 1, 6 }, card.GroupLessonOptions.Single(x => x.Label == "12 ders").Variants.Select(x => x.DurationMonths));
        Assert.Equal(2, card.OtherGroupVariants.Count(x => x.DurationMonths == 12));
        Assert.Equal(card.Variants.Count, card.GroupLessonOptions.SelectMany(x => x.Variants).Concat(card.OtherGroupVariants).Select(x => x.Id).Distinct().Count());
    }

    private static ApplicationDbContext Db() => new(new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
}
