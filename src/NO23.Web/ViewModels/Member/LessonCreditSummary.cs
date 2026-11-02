namespace NO23.Web.ViewModels.Member;

public sealed class LessonCreditSummary
{
    public IReadOnlyList<LessonCategorySummary> Categories { get; init; } = [];
    public bool HasCategoryAccounts { get; init; }
    public bool IsActivationPending { get; init; }
    public bool RequiresCategoryReview { get; init; }
    public int MemberId { get; init; }
    public bool HasSportsPackage { get; init; }
    public bool IsUnlimited { get; init; }
    public bool IsPackageActive { get; init; }
    public bool HasKnownPeriod { get; init; }
    public int? PackageTotal { get; init; }
    public int? PersonalTrainingTotal { get; init; }
    public int? GroupTotal { get; init; }
    public int PersonalTrainingCompleted { get; init; }
    public int PersonalTrainingPlanned { get; init; }
    public int GroupCompleted { get; init; }
    public int GroupPlanned { get; init; }
    public int Completed { get; init; }
    public int Planned { get; init; }
    public int Remaining { get; init; }
    public int Available { get; init; }
    public int StoredBalance { get; init; }
    public int UnchargedPersonal { get; init; }
    public int UnreviewedCompleted { get; init; }
    public int InvalidGroupReservations { get; init; }
    public int ConvertedCredits { get; init; }
    public int? EffectivePackageTotal => PackageTotal + ConvertedCredits;
    public bool HasTotalDifference => !IsActivationPending && PackageTotal.HasValue && !IsUnlimited && Remaining + Completed != EffectivePackageTotal;
    public bool NeedsReview => RequiresCategoryReview || UnreviewedCompleted > 0 || InvalidGroupReservations > 0 || HasTotalDifference || StoredBalance < 0;
}

public sealed record LessonCategorySummary(NO23.Web.Domain.Enums.LessonCategory Category, int Granted, int Converted, int Remaining, int Planned, int Available, int Completed, int? MonthlyLimit);
