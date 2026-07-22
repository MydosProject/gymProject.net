namespace NO23.Web.ViewModels.Classes;

public class UpcomingClassSessionPublicViewModel
{
    public int ClassSessionId { get; init; }

    public int GroupClassId { get; init; }

    public string ClassName { get; init; } = string.Empty;

    public string TrainerName { get; init; } = string.Empty;

    public DateTime StartsAtUtc { get; init; }

    public DateTime StartsAtLocal { get; init; }

    public int Capacity { get; init; }

    public int ReservedCount { get; init; }

    public int RemainingCapacity { get; init; }
}
