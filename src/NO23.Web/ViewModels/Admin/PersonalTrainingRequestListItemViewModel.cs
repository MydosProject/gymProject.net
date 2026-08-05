namespace NO23.Web.ViewModels.Admin;

public class PersonalTrainingRequestListItemViewModel
{
    public int Id { get; init; }

    public string MemberName { get; init; } = string.Empty;

    public string MemberEmail { get; init; } = string.Empty;

    public string TrainerName { get; init; } = string.Empty;

    public DateOnly PreferredDate { get; init; }

    public string PreferredTimeWindow { get; init; } = string.Empty;

    public string Status { get; init; } = string.Empty;

    public bool IsPending { get; init; }

    public DateTime CreatedAtUtc { get; init; }
}
