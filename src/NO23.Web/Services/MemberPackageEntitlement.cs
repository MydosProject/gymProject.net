using NO23.Web.Domain.Entities;
using NO23.Web.Domain.Enums;

namespace NO23.Web.Services;

public static class MemberPackageEntitlement
{
    public static bool IncludesPersonalTraining(MemberProfile profile) =>
        profile.MembershipPackage.Code != MembershipPackageCode.KitchenOnly &&
        (profile.LessonPeriodId.HasValue && profile.LessonAccounts.Any(a => a.PeriodId == profile.LessonPeriodId)
            ? profile.LessonAccounts.Any(a => a.PeriodId == profile.LessonPeriodId && a.Category == LessonCategory.PersonalTraining && a.Granted + a.ConvertedCredits > 0)
            : profile.ServicePackageVariant is { } variant ? variant.PersonalTrainingSessionCount > 0 :
         profile.MembershipPackageOption is { } option ? option.PersonalTrainingSessionCount > 0 :
         profile.MembershipPackage.IncludesPersonalTrainingSupport);

    public static bool IsActive(MemberProfile profile, DateTime atUtc) =>
        !profile.MembershipActivationPending &&
        profile.MembershipPackage.Code != MembershipPackageCode.KitchenOnly &&
        (!profile.MembershipEndsAtUtc.HasValue || profile.MembershipEndsAtUtc.Value > atUtc);

    public static DateTime CalculateEndDate(ServicePackageVariant variant, DateTime startsAtUtc) =>
        variant.DurationDays is > 0
            ? startsAtUtc.AddDays(variant.DurationDays.Value)
            : startsAtUtc.AddMonths(variant.DurationMonths ?? 1);

    public static string DurationLabel(ServicePackageVariant variant) =>
        variant.DurationDays is > 0 ? $"{variant.DurationDays} gün" : $"{variant.DurationMonths ?? 1} ay";

    public static int CalculatePersonalTrainingCredits(ServicePackageVariant variant) =>
        variant.PersonalTrainingSessionCount * CreditMultiplier(variant);

    public static int CalculateGroupCredits(ServicePackageVariant variant) =>
        (variant.ReformerClassCreditCount
            + variant.PerformanceClassCreditCount
            + variant.GroupClassCreditCount
            + variant.KidsClassCreditCount) * CreditMultiplier(variant);

    public static int CalculateInitialCredits(ServicePackageVariant variant) =>
        CalculatePersonalTrainingCredits(variant) + CalculateGroupCredits(variant);

    private static int CreditMultiplier(ServicePackageVariant variant) =>
        variant.LessonsRenewMonthly ? Math.Max(1, variant.DurationMonths ?? 1) : 1;

    public static bool HasUnlimitedClassAccess(MemberProfile profile) =>
        profile.LessonPeriodId is null &&
        profile.MembershipPackage.Code != MembershipPackageCode.KitchenOnly &&
        profile.ServicePackageVariantId is null && profile.MembershipPackage.WeeklyClassLimit is null;
}
