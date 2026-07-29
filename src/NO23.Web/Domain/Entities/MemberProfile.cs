namespace NO23.Web.Domain.Entities;

public class MemberProfile
{
    public int Id { get; set; }

    public string ApplicationUserId { get; set; } = string.Empty;

    public ApplicationUser ApplicationUser { get; set; } = null!;

    public int MembershipPackageId { get; set; }

    public MembershipPackage MembershipPackage { get; set; } = null!;

    public string? FitnessGoal { get; set; }

    public int RemainingClassCredits { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAtUtc { get; set; }

    public ICollection<ClassReservation> ClassReservations { get; set; } = [];

    public ICollection<CommunityChallengeParticipation> CommunityChallengeParticipations { get; set; } = [];

    public ICollection<KitchenSubscription> KitchenSubscriptions { get; set; } = [];

    public ShoppingCart? ShoppingCart { get; set; }

    public ICollection<Order> Orders { get; set; } = [];
}
