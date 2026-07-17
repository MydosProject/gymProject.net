namespace NO23.Web.ViewModels.Member;

public class MemberDashboardViewModel
{
    public string MemberName { get; init; } = string.Empty;

    public string PackageName { get; init; } = string.Empty;

    public int RemainingClassCredits { get; init; }

    public bool HasUnlimitedClasses { get; init; }

    public IReadOnlyList<MemberReservationViewModel> UpcomingReservations { get; init; } = [];

    public IReadOnlyList<AvailableClassSessionViewModel> AvailableSessions { get; init; } = [];
}
