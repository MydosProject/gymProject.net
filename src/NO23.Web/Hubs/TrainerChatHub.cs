using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using NO23.Web.Data.Seed;
using NO23.Web.Services;

namespace NO23.Web.Hubs;

[Authorize(
    Roles =
        ApplicationRoles.Member +
        "," +
        ApplicationRoles.Trainer)]
public class TrainerChatHub(
    TrainerMessagingService messagingService)
    : Hub
{

    public async Task<int> GetUnreadCount()
    {
        var userId =
            Context.User?
                .FindFirstValue(
                    ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new HubException(
                "Oturum bilgisi bulunamadı.");
        }

        return await messagingService
            .GetUnreadCountAsync(userId);
    }

    public async Task JoinConversation(
        int conversationId)
    {
        var userId =
            Context.User?
                .FindFirstValue(
                    ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new HubException(
                "Oturum bilgisi bulunamadı.");
        }

        var canAccess =
            await messagingService
                .CanAccessConversationAsync(
                    userId,
                    conversationId);

        if (!canAccess)
        {
            throw new HubException(
                "Bu konuşmaya erişemezsiniz.");
        }

        await Groups.AddToGroupAsync(
            Context.ConnectionId,
            GetConversationGroupName(
                conversationId));
    }

    public async Task LeaveConversation(
        int conversationId)
    {
        await Groups.RemoveFromGroupAsync(
            Context.ConnectionId,
            GetConversationGroupName(
                conversationId));
    }

    public async Task MarkConversationAsRead(
    int conversationId)
    {
        var userId =
            Context.User?
                .FindFirstValue(
                    ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new HubException(
                "Oturum bilgisi bulunamadı.");
        }

        var result =
            await messagingService
                .MarkConversationAsReadAsync(
                    userId,
                    conversationId);

        if (!result.Succeeded)
        {
            throw new HubException(
                result.ErrorMessage ??
                "Mesajlar okundu olarak işaretlenemedi.");
        }

        if (result.MessageIds.Count == 0)
        {
            return;
        }

        await Clients
            .Group(
                GetConversationGroupName(
                    conversationId))
            .SendAsync(
                "MessagesRead",
                new
                {
                    conversationId,

                    readerApplicationUserId =
                        userId,

                    messageIds =
                        result.MessageIds,

                    readAtUtc =
                        result.ReadAtUtc
                });

        await Clients
            .User(userId)
            .SendAsync(
                "RefreshUnreadCount");
    }

    public async Task SetTyping(
        int conversationId,
        bool isTyping)
    {
        var userId =
            Context.User?
                .FindFirstValue(
                    ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new HubException(
                "Oturum bilgisi bulunamadı.");
        }

        var canAccess =
            await messagingService
                .CanAccessConversationAsync(
                    userId,
                    conversationId);

        if (!canAccess)
        {
            throw new HubException(
                "Bu konuşmaya erişemezsiniz.");
        }

        await Clients
            .OthersInGroup(
                GetConversationGroupName(
                    conversationId))
            .SendAsync(
                "TypingChanged",
                new
                {
                    conversationId,
                    userId,
                    isTyping
                });
    }

    public static string GetConversationGroupName(
        int conversationId)
    {
        return
            $"trainer-conversation-{conversationId}";
    }
}