using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using NO23.Web.Data.Seed;
using NO23.Web.Services;
using NO23.Web.ViewModels.Notifications;

namespace NO23.Web.Hubs;

[Authorize(
    Roles =
        ApplicationRoles.Member +
        "," +
        ApplicationRoles.Trainer)]
public class UserNotificationHub(
    UserNotificationService notificationService)
    : Hub
{
    public async Task<int> GetUnreadCount()
    {
        var userId = GetCurrentUserId();

        return await notificationService
            .GetUnreadCountAsync(userId);
    }

    public async Task<
        IReadOnlyList<UserNotificationListItemViewModel>>
        GetRecent()
    {
        var userId = GetCurrentUserId();

        return await notificationService
            .GetRecentAsync(
                userId,
                10);
    }

    public async Task MarkAsRead(
        int notificationId)
    {
        var userId = GetCurrentUserId();

        var succeeded =
            await notificationService
                .MarkAsReadAsync(
                    userId,
                    notificationId);

        if (!succeeded)
        {
            throw new HubException(
                "Bildirim bulunamadı.");
        }

        var unreadCount =
            await notificationService
                .GetUnreadCountAsync(
                    userId);

        await Clients.User(userId)
            .SendAsync(
                "NotificationStateChanged",
                new
                {
                    unreadCount
                });
    }

    public async Task MarkAllAsRead()
    {
        var userId = GetCurrentUserId();

        await notificationService
            .MarkAllAsReadAsync(
                userId);

        await Clients.User(userId)
            .SendAsync(
                "NotificationStateChanged",
                new
                {
                    unreadCount = 0
                });
    }

    private string GetCurrentUserId()
    {
        var userId =
            Context.User?
                .FindFirstValue(
                    ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(
            userId))
        {
            throw new HubException(
                "Oturum bilgisi bulunamadı.");
        }

        return userId;
    }
}