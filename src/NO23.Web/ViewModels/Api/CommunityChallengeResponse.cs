namespace NO23.Web.ViewModels.Api;

public class CommunityChallengeResponse
{
    public int Id { get; init; }

    public string Title { get; init; } = string.Empty;

    public string Slug { get; init; } = string.Empty;

    public string Summary { get; init; } = string.Empty;

    public string Goal { get; init; } = string.Empty;

    public string? Reward { get; init; }

    public int TargetDailyCalories { get; init; }

    public decimal CalorieTolerancePercent { get; init; }

    public int RequiredCompletionPercent { get; init; }

    public DateOnly StartsOn { get; init; }

    public DateOnly EndsOn { get; init; }

    public string Status { get; init; } = string.Empty;

    public string? ImageUrl { get; init; }
}
