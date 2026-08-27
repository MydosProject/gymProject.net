using NO23.Web.Domain.Enums;

namespace NO23.Web.Domain.Entities;

public class ServicePackageApplication
{
    public int Id { get; set; }

    public int ServicePackageId { get; set; }

    public ServicePackage ServicePackage { get; set; } = null!;

    public int ServicePackageVariantId { get; set; }

    public ServicePackageVariant ServicePackageVariant { get; set; } = null!;

    public string? ApplicationUserId { get; set; }

    public ApplicationUser? ApplicationUser { get; set; }

    public string FullName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string PhoneNumber { get; set; } = string.Empty;

    public string? Notes { get; set; }

    public ServicePackageApplicationStatus Status { get; set; } =
        ServicePackageApplicationStatus.Pending;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAtUtc { get; set; }
}
