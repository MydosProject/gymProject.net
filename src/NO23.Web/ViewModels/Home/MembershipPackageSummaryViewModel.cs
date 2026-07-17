namespace NO23.Web.ViewModels.Home;

public class MembershipPackageSummaryViewModel
{
    public string Code { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    public string Audience { get; init; } = string.Empty;

    public IReadOnlyList<string> Features { get; init; } = [];

    public int DisplayOrder { get; init; }
}
