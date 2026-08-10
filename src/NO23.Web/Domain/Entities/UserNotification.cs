using NO23.Web.Domain.Enums;

namespace NO23.Web.Domain.Entities;

public class UserNotification
{
    public int Id { get; set; }

    public string ApplicationUserId { get; set; } =
        string.Empty;

    public ApplicationUser ApplicationUser { get; set; } =
        null!;

    public UserNotificationType Type { get; set; }

    public string Title { get; set; } =
        string.Empty;

    public string Message { get; set; } =
        string.Empty;

    public string? Url { get; set; }

    public int? RelatedEntityId { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime? ReadAtUtc { get; set; }
}