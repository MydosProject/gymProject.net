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

    public static string GetConversationGroupName(
        int conversationId)
    {
        return
            $"trainer-conversation-{conversationId}";
    }
}