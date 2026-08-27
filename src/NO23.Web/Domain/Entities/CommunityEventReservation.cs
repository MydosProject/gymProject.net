using NO23.Web.Domain.Enums;

namespace NO23.Web.Domain.Entities;

public class CommunityEventReservation
{
    public int Id { get; set; }

    public int CommunityEventId { get; set; }

    public CommunityEvent CommunityEvent { get; set; } = null!;

    public int MemberProfileId { get; set; }

    public MemberProfile MemberProfile { get; set; } = null!;

    public CommunityEventReservationStatus Status { get; set; } =
        CommunityEventReservationStatus.Reserved;

    public DateTime ReservedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime? CancelledAtUtc { get; set; }

    public string? CancellationReason { get; set; }
}
