using Microsoft.EntityFrameworkCore;
using NO23.Web.Data;
using NO23.Web.Domain.Entities;
using NO23.Web.Domain.Enums;
using NO23.Web.ViewModels.Notifications;

namespace NO23.Web.Services;

public class UserNotificationService(
    ApplicationDbContext dbContext)
{
    public async Task<UserNotification> CreateAsync(
        string applicationUserId,
        UserNotificationType type,
        string title,
        string message,
        string? url = null,
        int? relatedEntityId = null)
    {
        if (string.IsNullOrWhiteSpace(
            applicationUserId))
        {
            throw new ArgumentException(
                "Kullanıcı bilgisi gereklidir.",
                nameof(applicationUserId));
        }

        var notification =
            new UserNotification
            {
                ApplicationUserId =
                    applicationUserId,

                Type =
                    type,

                Title =
                    title.Trim(),

                Message =
                    message.Trim(),

                Url =
                    string.IsNullOrWhiteSpace(url)
                        ? null
                        : url,

                RelatedEntityId =
                    relatedEntityId,

                CreatedAtUtc =
                    DateTime.UtcNow
            };

        dbContext.UserNotifications.Add(
            notification);

        await dbContext.SaveChangesAsync();

        return notification;
    }

    public async Task<int> GetUnreadCountAsync(
        string applicationUserId)
    {
        return await dbContext
            .UserNotifications
            .AsNoTracking()
            .CountAsync(notification =>
                notification.ApplicationUserId ==
                    applicationUserId &&
                notification.ReadAtUtc == null);
    }

    public async Task<
        IReadOnlyList<UserNotificationListItemViewModel>>
        GetRecentAsync(
            string applicationUserId,
            int take = 10)
    {
        return await dbContext
            .UserNotifications
            .AsNoTracking()
            .Where(notification =>
                notification.ApplicationUserId ==
                applicationUserId)
            .OrderByDescending(notification =>
                notification.CreatedAtUtc)
            .Take(take)
            .Select(notification =>
                new UserNotificationListItemViewModel
                {
                    Id =
                        notification.Id,

                    Type =
                        notification.Type,

                    Title =
                        notification.Title,

                    Message =
                        notification.Message,

                    Url =
                        notification.Url,

                    CreatedAtUtc =
                        notification.CreatedAtUtc,

                    ReadAtUtc =
                        notification.ReadAtUtc
                })
            .ToListAsync();
    }

    public async Task<bool> MarkAsReadAsync(
        string applicationUserId,
        int notificationId)
    {
        var notification =
            await dbContext.UserNotifications
                .SingleOrDefaultAsync(item =>
                    item.Id ==
                        notificationId &&
                    item.ApplicationUserId ==
                        applicationUserId);

        if (notification is null)
        {
            return false;
        }

        if (notification.ReadAtUtc is null)
        {
            notification.ReadAtUtc =
                DateTime.UtcNow;

            await dbContext.SaveChangesAsync();
        }

        return true;
    }

    public async Task<int> MarkAllAsReadAsync(
        string applicationUserId)
    {
        var notifications =
            await dbContext.UserNotifications
                .Where(notification =>
                    notification.ApplicationUserId ==
                        applicationUserId &&
                    notification.ReadAtUtc == null)
                .ToListAsync();

        if (notifications.Count == 0)
        {
            return 0;
        }

        var nowUtc =
            DateTime.UtcNow;

        foreach (var notification in notifications)
        {
            notification.ReadAtUtc =
                nowUtc;
        }

        await dbContext.SaveChangesAsync();

        return notifications.Count;
    }
}