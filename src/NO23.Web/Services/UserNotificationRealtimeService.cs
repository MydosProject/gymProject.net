using Microsoft.AspNetCore.SignalR;
using NO23.Web.Domain.Enums;
using NO23.Web.Extensions;
using NO23.Web.Hubs;

namespace NO23.Web.Services;

public class UserNotificationRealtimeService(
    UserNotificationService notificationService,
    IHubContext<UserNotificationHub> hubContext)
{
    public async Task CreateAndPublishAsync(
        string applicationUserId,
        UserNotificationType type,
        string title,
        string message,
        string? url = null,
        int? relatedEntityId = null)
    {
        var notification =
            await notificationService
                .CreateAsync(
                    applicationUserId,
                    type,
                    title,
                    message,
                    url,
                    relatedEntityId);

        var unreadCount =
            await notificationService
                .GetUnreadCountAsync(
                    applicationUserId);

        await hubContext.Clients
            .User(applicationUserId)
            .SendAsync(
                "NotificationReceived",
                new
                {
                    id =
                        notification.Id,

                    type =
                        notification.Type,

                    title =
                        notification.Title,

                    message =
                        notification.Message,

                    url =
                        notification.Url,

                    createdAtUtc =
                        notification.CreatedAtUtc,

                    readAtUtc =
                        notification.ReadAtUtc,

                    unreadCount
                });
    }

    public async Task PublishPersonalTrainingChangedAsync(
    string applicationUserId,
    int requestId,
    PersonalTrainingRequestStatus status,
    DateTime? scheduledAtUtc = null,
    string? trainerName = null,
    string? trainerNote = null)
    {
        await hubContext.Clients
            .User(applicationUserId)
            .SendAsync(
                "PersonalTrainingChanged",
                new
                {
                    requestId,

                    status =
                        status.ToString(),

                    statusDisplayName =
                        status.GetDisplayName(),

                    scheduledAtUtc,

                    trainerName,

                    trainerNote
                });
    }
}
