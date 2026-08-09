namespace NO23.Web.Domain.Entities;

public class TrainerMessage
{
    public int Id { get; set; }

    public int TrainerConversationId { get; set; }

    public TrainerConversation TrainerConversation { get; set; } = null!;

    public string SenderApplicationUserId { get; set; } = string.Empty;

    public ApplicationUser SenderApplicationUser { get; set; } = null!;

    public string Body { get; set; } = string.Empty;

    public DateTime SentAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime? ReadAtUtc { get; set; }
}