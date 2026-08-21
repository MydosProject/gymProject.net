namespace NO23.Web.ViewModels.Membership;

public class MembershipOptionsViewModel
{
    public string PackageCode { get; init; } = string.Empty;
    public string PackageName { get; init; } = string.Empty;
    public string PackageAudience { get; init; } = string.Empty;
    public IReadOnlyList<MembershipServiceOptionViewModel> Options { get; init; } = [];
}

public class MembershipServiceOptionViewModel
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public int DurationDays { get; init; }
    public int PersonalTrainingSessionCount { get; init; }
    public int GroupClassCreditCount { get; init; }
    public bool IncludesGymAccess { get; init; }
    public int DisplayOrder { get; init; }
}
