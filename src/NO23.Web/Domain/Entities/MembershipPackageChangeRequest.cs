using NO23.Web.Domain.Enums;

namespace NO23.Web.Domain.Entities;

public class MembershipPackageChangeRequest
{
    public int Id { get; set; }

    public int MemberProfileId { get; set; }

    public MemberProfile MemberProfile { get; set; } = null!;

    public int CurrentMembershipPackageId { get; set; }

    public MembershipPackage CurrentMembershipPackage { get; set; } = null!;

    public int RequestedMembershipPackageId { get; set; }

    public MembershipPackage RequestedMembershipPackage { get; set; } = null!;

    public MembershipPackageChangeRequestStatus Status { get; set; } =
        MembershipPackageChangeRequestStatus.Pending;

    public DateTime RequestedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime? ResolvedAtUtc { get; set; }

    public string? ResolvedByUserId { get; set; }

    public ApplicationUser? ResolvedByUser { get; set; }

    public string? AdminNote { get; set; }
}
