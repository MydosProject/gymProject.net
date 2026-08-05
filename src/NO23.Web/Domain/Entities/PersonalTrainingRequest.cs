using NO23.Web.Domain.Enums;

namespace NO23.Web.Domain.Entities;

public class PersonalTrainingRequest
{
    public int Id { get; set; }

    public int MemberProfileId { get; set; }

    public MemberProfile MemberProfile { get; set; } = null!;

    public int TrainerId { get; set; }

    public Trainer Trainer { get; set; } = null!;

    public DateOnly PreferredDate { get; set; }

    public string PreferredTimeWindow { get; set; } = string.Empty;

    public string? GoalNote { get; set; }

    public PersonalTrainingRequestStatus Status { get; set; } =
        PersonalTrainingRequestStatus.Pending;

    public DateTime? ScheduledAtUtc { get; set; }

    public string? AdminNote { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAtUtc { get; set; }

    public DateTime? CancelledAtUtc { get; set; }
}
