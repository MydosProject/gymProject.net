using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NO23.Web.Controllers;
using NO23.Web.Data;
using NO23.Web.Data.Seed;
using NO23.Web.Domain.Entities;
using NO23.Web.Domain.Enums;
using NO23.Web.ViewModels.Membership;

namespace NO23.Tests;

public class MembershipPackageOptionTests
{
    [Fact]
    public void Defaults_ProvideServiceOptionsForEveryMembershipPackage()
    {
        foreach (var code in Enum.GetValues<MembershipPackageCode>())
        {
            var options = MembershipPackageOptionSeed.Defaults.Where(x => x.PackageCode == code).ToList();
            Assert.Contains(options, x => x.PersonalTrainingSessionCount > 0);
            Assert.Contains(options, x => x.GroupClassCreditCount > 0);
        }
    }

    [Fact]
    public void Defaults_HaveUniqueNamesWithinEachPackage()
    {
        Assert.All(MembershipPackageOptionSeed.Defaults.GroupBy(x => new { x.PackageCode, x.Name }),
            group => Assert.Single(group));
    }

    [Fact]
    public async Task Options_ReturnsOnlyActiveOptionsForSelectedPackage()
    {
        await using var dbContext = CreateDbContext();
        var package = new MembershipPackage
        {
            Code = MembershipPackageCode.Pro, Name = "PRO", Audience = "Hedef odaklı",
            Description = "Test", IsActive = true
        };
        package.Options.Add(new MembershipPackageOption
        {
            Name = "20 Gün PT", Description = "Aktif", DurationDays = 20,
            PersonalTrainingSessionCount = 8, IsActive = true, DisplayOrder = 1
        });
        package.Options.Add(new MembershipPackageOption
        {
            Name = "Eski Seçenek", Description = "Pasif", DurationDays = 10,
            GroupClassCreditCount = 4, IsActive = false, DisplayOrder = 2
        });
        dbContext.MembershipPackages.Add(package);
        await dbContext.SaveChangesAsync();

        var result = await new MembershipController(dbContext).Options("PRO");

        var view = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<MembershipOptionsViewModel>(view.Model);
        Assert.Equal("20 Gün PT", Assert.Single(model.Options).Name);
    }

    private static ApplicationDbContext CreateDbContext() => new(
        new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
}
