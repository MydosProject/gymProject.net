using NO23.Web.Domain.Enums;

namespace NO23.Web.ViewModels.TrainerPanel;

public class TrainerPersonalTrainingRequestListItemViewModel
{
    public int Id { get; set; }

    public string MemberName { get; set; } = string.Empty;

    public string MemberEmail { get; set; } = string.Empty;

    public DateOnly PreferredDate { get; set; }

    public string PreferredTimeWindow { get; set; } = string.Empty;

    public string? GoalNote { get; set; }

    public PersonalTrainingRequestStatus Status { get; set; }

    public string StatusDisplayName { get; set; } = string.Empty;

    public DateTime? ScheduledAtUtc { get; set; }

    public DateTime CreatedAtUtc { get; set; }
}
