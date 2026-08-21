using NO23.Web.Domain.Enums;

namespace NO23.Web.Domain.Entities;

public class MembershipPackage
{
    public int Id { get; set; }

    public MembershipPackageCode Code { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Audience { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public int? WeeklyClassLimit { get; set; }

    public bool IncludesMeasurement { get; set; }

    public bool IncludesBodyAnalysis { get; set; }

    public bool IncludesNutritionSupport { get; set; }

    public bool IncludesDetailedTracking { get; set; }

    public bool IncludesMonthlyAnalysis { get; set; }

    public bool IncludesPriorityReservation { get; set; }

    public bool IncludesPersonalTrainingSupport { get; set; }

    public bool IncludesKitchenBenefits { get; set; }

    public bool IncludesPrivateEvents { get; set; }

    public bool IncludesCommunityMembership { get; set; }

    public bool IsActive { get; set; } = true;

    public int DisplayOrder { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAtUtc { get; set; }

    public ICollection<MemberProfile> MemberProfiles { get; set; } = [];

    public ICollection<MembershipPackageOption> Options { get; set; } = [];
}
