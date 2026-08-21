namespace NO23.Web.Domain.Entities;

public class MembershipPackageOption
{
    public int Id { get; set; }
    public int MembershipPackageId { get; set; }
    public MembershipPackage MembershipPackage { get; set; } = null!;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int DurationDays { get; set; }
    public int PersonalTrainingSessionCount { get; set; }
    public int GroupClassCreditCount { get; set; }
    public bool IncludesGymAccess { get; set; }
    public bool IsActive { get; set; } = true;
    public int DisplayOrder { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }
    public ICollection<MemberProfile> MemberProfiles { get; set; } = [];
}
