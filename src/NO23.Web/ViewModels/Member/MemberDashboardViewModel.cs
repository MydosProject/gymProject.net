namespace NO23.Web.ViewModels.Member;

public class MemberDashboardViewModel
{
    public string MemberName { get; init; } = string.Empty;

    public string PackageName { get; init; } = string.Empty;

    public string MembershipSummaryLabel { get; init; } = "Üyelik bilgisi";

    public string MembershipSummaryTitle { get; init; } =
        "Paket bilgisi bulunamadı";

    public string MembershipSummaryDescription { get; init; } =
        "Üyelik kaydın görüntülenemiyor.";

    public string LastMembershipPackageName { get; init; } = string.Empty;

    public DateTime? MembershipEndsAtUtc { get; init; }

    public int RemainingClassCredits { get; init; }

    public bool HasUnlimitedClasses { get; init; }

    public bool HasActiveKitchenSubscription { get; init; }

    public IReadOnlyList<MemberReservationViewModel> UpcomingReservations { get; init; } = [];

    public IReadOnlyList<AvailableClassSessionViewModel> AvailableSessions { get; init; } = [];
}
