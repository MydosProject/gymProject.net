namespace NO23.Web.Domain.Entities;

public class TrainerConversation
{
    public int Id { get; set; }

    public int MemberProfileId { get; set; }

    public MemberProfile MemberProfile { get; set; } = null!;

    public int TrainerId { get; set; }

    public Trainer Trainer { get; set; } = null!;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime? LastMessageAtUtc { get; set; }

    public ICollection<TrainerMessage> Messages { get; set; } = [];
}