using NO23.Web.Domain.Enums;

namespace NO23.Web.Domain.Entities;

public class CommunityChallengeParticipation
{
    public int Id { get; set; }

    public int CommunityChallengeId { get; set; }

    public CommunityChallenge CommunityChallenge { get; set; } = null!;

    public int MemberProfileId { get; set; }

    public MemberProfile MemberProfile { get; set; } = null!;

    public CommunityChallengeParticipationStatus Status { get; set; } =
        CommunityChallengeParticipationStatus.Active;

    public DateTime JoinedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime? CompletedAtUtc { get; set; }

    public DateTime? UpdatedAtUtc { get; set; }

    public ICollection<ChallengeProgressEntry> ProgressEntries { get; set; } = [];
}
