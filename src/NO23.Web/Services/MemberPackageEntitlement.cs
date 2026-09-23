using NO23.Web.Domain.Entities;

namespace NO23.Web.Services;

public static class MemberPackageEntitlement
{
    public static bool IsActive(MemberProfile profile, DateTime atUtc) =>
        !profile.MembershipEndsAtUtc.HasValue || profile.MembershipEndsAtUtc.Value > atUtc;

    public static DateTime CalculateEndDate(ServicePackageVariant variant, DateTime startsAtUtc) =>
        variant.DurationDays is > 0
            ? startsAtUtc.AddDays(variant.DurationDays.Value)
            : startsAtUtc.AddMonths((variant.DurationMonths ?? 1) + variant.BonusMonths);

    public static int CalculateInitialCredits(ServicePackageVariant variant)
    {
        var credits = variant.PersonalTrainingSessionCount
            + variant.ReformerClassCreditCount
            + variant.PerformanceClassCreditCount
            + variant.GroupClassCreditCount
            + variant.KidsClassCreditCount;

        return variant.LessonsRenewMonthly && variant.DurationMonths > 1
            ? credits * variant.DurationMonths.Value
            : credits;
    }

    public static bool HasUnlimitedClassAccess(MemberProfile profile) =>
        profile.ServicePackageVariantId is null && profile.MembershipPackage.WeeklyClassLimit is null;
}
