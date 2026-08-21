using NO23.Web.Domain.Enums;

namespace NO23.Web.Domain.Entities;

public class PersonalTrainingSession
{
    public int Id { get; set; }
    public int TrainerId { get; set; }
    public Trainer Trainer { get; set; } = null!;
    public int MemberProfileId { get; set; }
    public MemberProfile MemberProfile { get; set; } = null!;
    public DateTime StartsAtUtc { get; set; }
    public int DurationMinutes { get; set; } = 60;
    public PersonalTrainingSessionStatus Status { get; set; } = PersonalTrainingSessionStatus.Scheduled;
    public bool CreditConsumed { get; set; }
    public string? Note { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }
    public ICollection<PersonalTrainingSessionHistory> History { get; set; } = [];
}
