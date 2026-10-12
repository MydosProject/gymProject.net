using Microsoft.AspNetCore.Mvc;
using NO23.Web.Controllers;
using NO23.Web.Data.Seed;
using NO23.Web.Domain.Enums;

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

    [Theory]
    [InlineData("START")]
    [InlineData("PLUS")]
    [InlineData("ELITE")]
    [InlineData("HYBRID")]
    [InlineData("PRO")]
    [InlineData("BLACK")]
    [InlineData("unknown")]
    [InlineData(null)]
    public void Options_RedirectsOldLinksToCurrentMembershipCatalog(string? package)
    {
        var result = new MembershipController().Options(package);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Plans", redirect.ControllerName);
        Assert.Equal(nameof(PlansController.Index), redirect.ActionName);
        Assert.Equal("membership", redirect.RouteValues?["category"]);
    }
}
