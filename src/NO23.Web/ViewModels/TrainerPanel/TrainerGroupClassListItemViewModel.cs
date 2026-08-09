namespace NO23.Web.ViewModels.TrainerPanel;

public class TrainerGroupClassListItemViewModel
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string DifficultyLevel { get; set; } = string.Empty;

    public int DurationMinutes { get; set; }

    public int AverageCaloriesBurned { get; set; }

    public int Capacity { get; set; }

    public bool IsActive { get; set; }

    public int UpcomingSessionCount { get; set; }

    public DateTime? NextSessionAtUtc { get; set; }
}