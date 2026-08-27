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

    public IReadOnlyList<ClassSessionParticipantViewModel> Participants { get; init; } = [];

    public string Status { get; init; } = string.Empty;

    public bool IsScheduled { get; init; }
}

public class ClassSessionParticipantViewModel
{
    public int ReservationId { get; init; }

    public string? FirstName { get; init; }

    public string? LastName { get; init; }

    public string Email { get; init; } = string.Empty;

    public string PackageName { get; init; } = string.Empty;

    public DateTime ReservedAtUtc { get; init; }

    public string DisplayName
    {
        get
        {
            var fullName = $"{FirstName} {LastName}".Trim();
            return string.IsNullOrWhiteSpace(fullName) ? Email : fullName;
        }
    }
}
