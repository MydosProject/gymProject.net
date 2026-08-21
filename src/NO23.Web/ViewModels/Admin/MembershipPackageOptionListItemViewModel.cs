namespace NO23.Web.ViewModels.Admin;

public class MembershipPackageOptionListItemViewModel
{
    public int Id { get; init; }
    public string PackageName { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public int DurationDays { get; init; }
    public int PersonalTrainingSessionCount { get; init; }
    public int GroupClassCreditCount { get; init; }
    public bool IncludesGymAccess { get; init; }
    public bool IsActive { get; init; }
    public int DisplayOrder { get; init; }
    public int MemberCount { get; init; }
}
