namespace NO23.Web.ViewModels.Admin;

public class TrainerListItemViewModel
{
    public int Id { get; init; }

    public string FullName { get; init; } = string.Empty;

    public string Specialty { get; init; } = string.Empty;

    public string? Certifications { get; init; }

    public int ClassCount { get; init; }

    public bool IsActive { get; init; }

    public bool HasPanelAccount { get; init; }
}
