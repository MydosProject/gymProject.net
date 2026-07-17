namespace NO23.Web.ViewModels.Api;

public class MembershipPackageResponse
{
    public int Id { get; init; }

    public string Code { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    public string Audience { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public int? WeeklyClassLimit { get; init; }

    public int DisplayOrder { get; init; }
}
