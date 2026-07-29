using NO23.Web.Domain.Enums;

namespace NO23.Web.Domain.Entities;

public class CommunityChallenge
{
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Slug { get; set; } = string.Empty;

    public string Summary { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string Goal { get; set; } = string.Empty;

    public string? Reward { get; set; }

    public int TargetDailyCalories { get; set; } = 2000;

    public decimal CalorieTolerancePercent { get; set; } = 10;

    public int RequiredCompletionPercent { get; set; } = 80;

    public DateOnly StartsOn { get; set; }

    public DateOnly EndsOn { get; set; }

    public CommunityChallengeStatus Status { get; set; } = CommunityChallengeStatus.Upcoming;

    public string? ImageUrl { get; set; }

    public int DisplayOrder { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAtUtc { get; set; }

    public ICollection<CommunityChallengeParticipation> Participations { get; set; } = [];
}
