namespace NO23.Web.ViewModels.Admin;

public class GroupClassListItemViewModel
{
    public int Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public string TrainerName { get; init; } = string.Empty;

    public string DifficultyLevel { get; init; } = string.Empty;

    public int DurationMinutes { get; init; }

    public int AverageCaloriesBurned { get; init; }

    public int Capacity { get; init; }

    public int SessionCount { get; init; }

    public bool IsActive { get; init; }
}
