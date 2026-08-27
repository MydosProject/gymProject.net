using NO23.Web.Domain.Enums;

namespace NO23.Web.ViewModels.Plans;

public class ServicePackageCatalogViewModel
{
    public ServicePackageCategory Category { get; init; }
    public string CategoryTitle { get; init; } = string.Empty;
    public string Headline { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public IReadOnlyList<ServicePackageCardViewModel> Packages { get; init; } = [];
}

public class ServicePackageCardViewModel
{
    public string Slug { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Subtitle { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public bool IsFeatured { get; init; }
    public string? MembershipCode { get; init; }
    public IReadOnlyList<string> Features { get; init; } = [];
    public IReadOnlyList<ServicePackageVariantCardViewModel> Variants { get; init; } = [];
}

public class ServicePackageVariantCardViewModel
{
    public int Id { get; init; }

    public string Name { get; init; } = string.Empty;
    public string Price { get; init; } = string.Empty;
    public string PriceNote { get; init; } = string.Empty;
    public string Rights { get; init; } = string.Empty;
    public bool IsRecommended { get; init; }
}
