using NO23.Web.Domain.Enums;

namespace NO23.Web.ViewModels.Admin;

public class ServicePackageApplicationListItemViewModel
{
    public int Id { get; init; }

    public string PackageCategory { get; init; } = string.Empty;

    public string PackageName { get; init; } = string.Empty;

    public string VariantName { get; init; } = string.Empty;

    public string FullName { get; init; } = string.Empty;

    public string Email { get; init; } = string.Empty;

    public string PhoneNumber { get; init; } = string.Empty;

    public string? Notes { get; init; }

    public ServicePackageApplicationStatus Status { get; init; }

    public DateTime CreatedAtLocal { get; init; }
}
