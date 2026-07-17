using NO23.Web.Domain.Enums;

namespace NO23.Web.Domain.Entities;

public class ClassSession
{
    public int Id { get; set; }

    public int GroupClassId { get; set; }

    public GroupClass GroupClass { get; set; } = null!;

    public DateTime StartsAtUtc { get; set; }

    public int? CapacityOverride { get; set; }

    public ClassSessionStatus Status { get; set; } = ClassSessionStatus.Scheduled;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAtUtc { get; set; }

    public ICollection<ClassReservation> Reservations { get; set; } = [];
}
