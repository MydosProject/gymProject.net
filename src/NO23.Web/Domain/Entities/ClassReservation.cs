using NO23.Web.Domain.Enums;

namespace NO23.Web.Domain.Entities;

public class ClassReservation
{
    public int Id { get; set; }

    public int ClassSessionId { get; set; }

    public ClassSession ClassSession { get; set; } = null!;

    public int MemberProfileId { get; set; }

    public MemberProfile MemberProfile { get; set; } = null!;

    public ClassReservationStatus Status { get; set; } = ClassReservationStatus.Reserved;

    public DateTime ReservedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime? CancelledAtUtc { get; set; }

    public string? CancellationReason { get; set; }
}
