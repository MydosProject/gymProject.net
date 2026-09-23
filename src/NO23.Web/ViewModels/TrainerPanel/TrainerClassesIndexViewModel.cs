namespace NO23.Web.ViewModels.TrainerPanel;

public class TrainerClassesIndexViewModel
{
    public IReadOnlyList<TrainerGroupClassListItemViewModel> GroupClasses { get; init; } = [];
    public IReadOnlyList<TrainerPersonalClassListItemViewModel> PersonalClasses { get; init; } = [];
}

public class TrainerPersonalClassListItemViewModel
{
    public int Id { get; init; }
    public string MemberName { get; init; } = string.Empty;
    public DateTime StartsAtUtc { get; init; }
    public int DurationMinutes { get; init; }
    public string Status { get; init; } = string.Empty;
}
