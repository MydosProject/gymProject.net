namespace NO23.Web.ViewModels.Admin;

public class MemberListItemViewModel
{
    public string Id { get; init; } = string.Empty;

    public string FullName { get; init; } = string.Empty;

    public string Email { get; init; } = string.Empty;

    public string? PhoneNumber { get; init; }

    public string PackageName { get; init; } = string.Empty;

    public string? FitnessGoal { get; init; }

    public int RemainingClassCredits { get; init; }

    public DateTime CreatedAtUtc { get; init; }
}
