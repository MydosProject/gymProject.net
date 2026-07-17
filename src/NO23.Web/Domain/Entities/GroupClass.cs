using NO23.Web.Domain.Enums;

namespace NO23.Web.Domain.Entities;

public class GroupClass
{
    public int Id { get; set; }

    public int TrainerId { get; set; }

    public Trainer Trainer { get; set; } = null!;

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public int DurationMinutes { get; set; }

    public ClassDifficultyLevel DifficultyLevel { get; set; }

    public int AverageCaloriesBurned { get; set; }

    public int Capacity { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAtUtc { get; set; }

    public ICollection<ClassSession> Sessions { get; set; } = [];
}
