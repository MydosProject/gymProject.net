using NO23.Web.Domain.Enums;

namespace NO23.Web.Domain.Entities;

public class ServicePackageVariant
{
    public int Id { get; set; }
    public int ServicePackageId { get; set; }
    public ServicePackage ServicePackage { get; set; } = null!;
    public string Name { get; set; } = string.Empty;
    public ServicePackageBillingType BillingType { get; set; }
    public int? DurationMonths { get; set; }
    public int? DurationDays { get; set; }
    public decimal? MonthlyPrice { get; set; }
    public decimal TotalPrice { get; set; }
    public int PersonalTrainingSessionCount { get; set; }
    public int ReformerClassCreditCount { get; set; }
    public int PerformanceClassCreditCount { get; set; }
    public int GroupClassCreditCount { get; set; }
    public int KidsClassCreditCount { get; set; }
    public bool IncludesGymAccess { get; set; }
    public bool IsRecommended { get; set; }
    public bool IsActive { get; set; } = true;
    public int DisplayOrder { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }
}
