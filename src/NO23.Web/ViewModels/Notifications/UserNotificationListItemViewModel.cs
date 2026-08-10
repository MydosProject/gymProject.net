using NO23.Web.Domain.Enums;

namespace NO23.Web.ViewModels.Notifications;

public class UserNotificationListItemViewModel
{
    public int Id { get; init; }

    public UserNotificationType Type { get; init; }

    public string Title { get; init; } =
        string.Empty;

    public string Message { get; init; } =
        string.Empty;

    public string? Url { get; init; }

    public DateTime CreatedAtUtc { get; init; }

    public DateTime? ReadAtUtc { get; init; }

    public bool IsRead =>
        ReadAtUtc.HasValue;
}