using NO23.Web.Domain.Enums;

namespace NO23.Web.Domain.Entities;

public class MemberProfile
{
    public const int DefaultMembershipDurationDays = 28;

    public int Id { get; set; }

    public string ApplicationUserId { get; set; } = string.Empty;

    public ApplicationUser ApplicationUser { get; set; } = null!;

    public int MembershipPackageId { get; set; }

    public MembershipPackage MembershipPackage { get; set; } = null!;

    public string? FitnessGoal { get; set; }

    public int RemainingClassCredits { get; set; }

    public bool IsSuspended { get; set; }

    public DateTime? SuspendedAtUtc { get; set; }

    public string? SuspensionReason { get; set; }

    public DateTime MembershipStartsAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime MembershipEndsAtUtc { get; set; } =
        DateTime.UtcNow.AddDays(DefaultMembershipDurationDays);

    public MembershipStatus MembershipStatus { get; set; } =
        MembershipStatus.Active;

    public DateTime? MembershipCancellationRequestedAtUtc { get; set; }

    public DateTime? MembershipCancellationEffectiveAtUtc { get; set; }

    public string? MembershipCancellationReason { get; set; }

    public string? IyzicoCustomerReferenceCode { get; set; }

    public string? IyzicoSubscriptionReferenceCode { get; set; }

    public string? IyzicoPricingPlanReferenceCode { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAtUtc { get; set; }

    public ICollection<ClassReservation> ClassReservations { get; set; } = [];

    public ICollection<PersonalTrainingRequest> PersonalTrainingRequests { get; set; } = [];

    public ICollection<MembershipPackageChangeRequest> MembershipPackageChangeRequests { get; set; } = [];

    public ICollection<TrainerConversation> TrainerConversations { get; set; } = [];

    public ICollection<CommunityChallengeParticipation> CommunityChallengeParticipations { get; set; } = [];

    public ICollection<MemberProgressEntry> ProgressEntries { get; set; } = [];

    public ICollection<KitchenSubscription> KitchenSubscriptions { get; set; } = [];

    public ShoppingCart? ShoppingCart { get; set; }

    public ICollection<Order> Orders { get; set; } = [];
}
