namespace NO23.Web.ViewModels.Member;

public class PersonalTrainingRequestListItemViewModel
{
    public int Id { get; init; }

    public string TrainerName { get; init; } = string.Empty;

    public string PreferredTimeWindow { get; init; } = string.Empty;

    public DateOnly PreferredDate { get; init; }

    public string Status { get; init; } = string.Empty;

    public DateTime? ScheduledAtUtc { get; init; }

    public string? AdminNote { get; init; }

    public bool CanCancel { get; init; }
}
