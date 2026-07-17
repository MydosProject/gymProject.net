namespace NO23.Web.ViewModels.Api;

public class ClassSessionResponse
{
    public int Id { get; init; }

    public string ClassName { get; init; } = string.Empty;

    public string TrainerName { get; init; } = string.Empty;

    public DateTime StartsAtUtc { get; init; }

    public int Capacity { get; init; }

    public int ReservedCount { get; init; }

    public int DurationMinutes { get; init; }

    public string DifficultyLevel { get; init; } = string.Empty;

    public int AverageCaloriesBurned { get; init; }
}
