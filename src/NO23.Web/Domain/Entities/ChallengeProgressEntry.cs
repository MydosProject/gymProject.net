namespace NO23.Web.Domain.Entities;

public class ChallengeProgressEntry
{
    public int Id { get; set; }

    public int CommunityChallengeParticipationId { get; set; }

    public CommunityChallengeParticipation CommunityChallengeParticipation { get; set; } = null!;

    public DateOnly EntryDate { get; set; }

    public int CaloriesConsumed { get; set; }

    public int TargetDailyCaloriesSnapshot { get; set; }

    public decimal CalorieTolerancePercentSnapshot { get; set; }

    public int MinCaloriesSnapshot { get; set; }

    public int MaxCaloriesSnapshot { get; set; }

    public bool IsCompliant { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAtUtc { get; set; }
}
