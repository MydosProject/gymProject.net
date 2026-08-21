using NO23.Web.Domain.Enums;

namespace NO23.Web.Domain.Entities;

public class ServicePackage
{
    public int Id { get; set; }
    public ServicePackageCategory Category { get; set; }
    public string Slug { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Subtitle { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsFeatured { get; set; }
    public bool IsActive { get; set; } = true;
    public int DisplayOrder { get; set; }
    public int? MembershipPackageId { get; set; }
    public MembershipPackage? MembershipPackage { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }
    public ICollection<ServicePackageFeature> Features { get; set; } = [];
    public ICollection<ServicePackageVariant> Variants { get; set; } = [];
}
