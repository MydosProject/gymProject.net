namespace NO23.Web.ViewModels.Classes;

public class GroupClassPublicViewModel
{
    public int GroupClassId { get; init; }

    public string Name { get; init; } = string.Empty;

    public string? Description { get; init; }

    public int DurationMinutes { get; init; }

    public string DifficultyLevel { get; init; } = string.Empty;

    public int AverageCaloriesBurned { get; init; }

    public string TrainerName { get; init; } = string.Empty;

    public IReadOnlyList<UpcomingClassSessionPublicViewModel> UpcomingSessions { get; init; } = [];
}
