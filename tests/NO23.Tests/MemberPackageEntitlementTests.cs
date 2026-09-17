using NO23.Web.Domain.Entities;
using NO23.Web.Services;

namespace NO23.Tests;

public class MemberPackageEntitlementTests
{
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
