using Microsoft.EntityFrameworkCore;
using NO23.Web.Data;
using NO23.Web.Domain.Enums;
using NO23.Web.Services;

namespace NO23.Tests;

public class UserNotificationServiceTests
{
    [Fact]
    public async Task MarkAsReadAsync_MarksNotificationAndUpdatesUnreadCount()
    {
        await using var dbContext =
            CreateDbContext();

        var service =
            new UserNotificationService(
                dbContext);

        var notification =
            await service.CreateAsync(
                "member-1",
                UserNotificationType.GroupClassSessionChanged,
                "Ders saati degisti",
                "Yeni zaman bilgisi.");

        var succeeded =
            await service.MarkAsReadAsync(
                "member-1",
                notification.Id);

        var unreadCount =
            await service.GetUnreadCountAsync(
                "member-1");

        Assert.True(succeeded);
        Assert.Equal(
            0,
            unreadCount);
        Assert.NotNull(
            await dbContext.UserNotifications
                .Where(item => item.Id == notification.Id)
                .Select(item => item.ReadAtUtc)
                .SingleAsync());
    }

    [Fact]
    public async Task MarkAsReadAsync_DoesNotMarkAnotherUsersNotification()
    {
        await using var dbContext =
            CreateDbContext();

        var service =
            new UserNotificationService(
                dbContext);

        var notification =
            await service.CreateAsync(
                "member-1",
                UserNotificationType.GroupClassSessionChanged,
                "Ders saati degisti",
                "Yeni zaman bilgisi.");

        var succeeded =
            await service.MarkAsReadAsync(
                "member-2",
                notification.Id);

        var unreadCount =
            await service.GetUnreadCountAsync(
                "member-1");

        Assert.False(succeeded);
        Assert.Equal(
            1,
            unreadCount);
    }

    [Fact]
    public async Task MarkAllAsReadAsync_ClearsOnlyCurrentUsersUnreadCount()
    {
        await using var dbContext =
            CreateDbContext();

        var service =
            new UserNotificationService(
                dbContext);

        await service.CreateAsync(
            "member-1",
            UserNotificationType.GroupClassSessionChanged,
            "Ders saati degisti",
            "Yeni zaman bilgisi.");

        await service.CreateAsync(
            "member-1",
            UserNotificationType.GroupClassSessionCancelled,
            "Ders iptal edildi",
            "Seans iptal edildi.");

        await service.CreateAsync(
            "member-2",
            UserNotificationType.GroupClassSessionCancelled,
            "Ders iptal edildi",
            "Seans iptal edildi.");

        var markedCount =
            await service.MarkAllAsReadAsync(
                "member-1");

        Assert.Equal(
            2,
            markedCount);
        Assert.Equal(
            0,
            await service.GetUnreadCountAsync(
                "member-1"));
        Assert.Equal(
            1,
            await service.GetUnreadCountAsync(
                "member-2"));
    }

    private static ApplicationDbContext CreateDbContext()
    {
        var options =
            new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(
                    Guid.NewGuid().ToString())
                .Options;

        return new ApplicationDbContext(
            options);
    }
}
