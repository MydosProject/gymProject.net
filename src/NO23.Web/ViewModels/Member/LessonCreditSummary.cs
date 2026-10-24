namespace NO23.Web.ViewModels.Member;

public sealed class LessonCreditSummary
{
    public int MemberId { get; init; }
    public bool HasSportsPackage { get; init; }
    public bool IsUnlimited { get; init; }
    public bool IsPackageActive { get; init; }
    public bool HasKnownPeriod { get; init; }
    public int? PackageTotal { get; init; }
    public int Completed { get; init; }
    public int Planned { get; init; }
    public int Remaining { get; init; }
    public int Available { get; init; }
    public int StoredBalance { get; init; }
    public int UnchargedPersonal { get; init; }
    public int UnreviewedCompleted { get; init; }
    public int InvalidGroupReservations { get; init; }
    public bool HasTotalDifference => PackageTotal.HasValue && !IsUnlimited && Remaining + Completed != PackageTotal;
    public bool NeedsReview => UnreviewedCompleted > 0 || InvalidGroupReservations > 0 || HasTotalDifference || StoredBalance < 0;
}
