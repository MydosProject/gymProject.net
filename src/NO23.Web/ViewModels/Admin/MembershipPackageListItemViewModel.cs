namespace NO23.Web.ViewModels.Admin;

public class MembershipPackageListItemViewModel
{
    public int Id { get; init; }

    public string Code { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    public string Audience { get; init; } = string.Empty;

    public int? WeeklyClassLimit { get; init; }

    public bool IsActive { get; init; }

    public int MemberCount { get; init; }

    public int DisplayOrder { get; init; }
}
