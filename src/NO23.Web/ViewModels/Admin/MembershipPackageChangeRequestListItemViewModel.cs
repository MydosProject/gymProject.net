namespace NO23.Web.ViewModels.Admin;

public class MembershipPackageChangeRequestListItemViewModel
{
    public int Id { get; init; }

    public string MemberName { get; init; } = string.Empty;

    public string MemberEmail { get; init; } = string.Empty;

    public string CurrentPackageName { get; init; } = string.Empty;

    public string RequestedPackageName { get; init; } = string.Empty;

    public string Status { get; init; } = string.Empty;

    public string StatusCssClass { get; init; } = string.Empty;

    public bool IsPending { get; init; }

    public DateTime RequestedAtUtc { get; init; }

    public DateTime? ResolvedAtUtc { get; init; }

    public string? AdminNote { get; init; }
}
