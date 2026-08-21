using NO23.Web.Domain.Enums;

namespace NO23.Web.Domain.Entities;

public class PersonalTrainingSessionHistory
{
    public int Id { get; set; }
    public int PersonalTrainingSessionId { get; set; }
    public PersonalTrainingSession PersonalTrainingSession { get; set; } = null!;
    public PersonalTrainingSessionStatus PreviousStatus { get; set; }
    public PersonalTrainingSessionStatus NewStatus { get; set; }
    public DateTime PreviousStartsAtUtc { get; set; }
    public DateTime NewStartsAtUtc { get; set; }
    public string? Note { get; set; }
    public string ChangedByUserId { get; set; } = string.Empty;
    public DateTime ChangedAtUtc { get; set; } = DateTime.UtcNow;
}
