namespace NO23.Web.ViewModels.Admin;

public class ClassSessionListItemViewModel
{
    public int Id { get; init; }

    public string ClassName { get; init; } = string.Empty;

    public string TrainerName { get; init; } = string.Empty;

    public bool IsGroupClassActive { get; init; }

    public DateTime StartsAtUtc { get; init; }

    public int Capacity { get; init; }

    public int ReservedCount { get; init; }

    public string Status { get; init; } = string.Empty;

    public bool IsScheduled { get; init; }
}
