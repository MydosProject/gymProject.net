namespace NO23.Web.ViewModels.Member;

public class MemberReservationViewModel
{
    public int ReservationId { get; init; }

    public string ClassName { get; init; } = string.Empty;

    public string TrainerName { get; init; } = string.Empty;

    public DateTime StartsAtUtc { get; init; }

    public bool CanCancel { get; init; }
}
