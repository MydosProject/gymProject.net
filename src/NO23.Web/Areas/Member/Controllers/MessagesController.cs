using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NO23.Web.Data;
using NO23.Web.Data.Seed;
using NO23.Web.Services;
using NO23.Web.ViewModels.Member;
using Microsoft.AspNetCore.SignalR;
using NO23.Web.Hubs;

namespace NO23.Web.Areas.Member.Controllers;

[Area("Member")]
[Authorize(Roles = ApplicationRoles.Member)]
public class MessagesController(
    ApplicationDbContext dbContext,
    TrainerMessagingService messagingService,
    IHubContext<TrainerChatHub> hubContext)
    : Controller
{
    public async Task<IActionResult> Index(
        int? conversationId)
    {
        var userId =
            User.FindFirstValue(
                ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Challenge();
        }

        var memberProfileId =
            await dbContext.MemberProfiles
                .AsNoTracking()
                .Where(profile =>
                    profile.ApplicationUserId ==
                    userId)
                .Select(profile =>
                    (int?)profile.Id)
                .FirstOrDefaultAsync();

        if (memberProfileId is null)
        {
            return Forbid();
        }

        var conversations =
            await dbContext.TrainerConversations
                .AsNoTracking()
                .Where(conversation =>
                    conversation.MemberProfileId ==
                    memberProfileId.Value)
                .OrderByDescending(conversation =>
                    conversation.LastMessageAtUtc ??
                    conversation.CreatedAtUtc)
                .Select(conversation =>
                    new MemberConversationListItemViewModel
                    {
                        Id =
                            conversation.Id,

                        TrainerName =
                            conversation.Trainer.FirstName +
                            " " +
                            conversation.Trainer.LastName,

                        Specialty =
                            conversation.Trainer.Specialty,

                        LastMessage =
                            conversation.Messages
                                .OrderByDescending(message =>
                                    message.SentAtUtc)
                                .Select(message =>
                                    message.Body)
                                .FirstOrDefault(),

                        LastActivityAtUtc =
                            conversation.LastMessageAtUtc ??
                            conversation.CreatedAtUtc
                    })
                .ToListAsync();

        if (conversations.Count == 0)
        {
            return View(
                new MemberMessagesViewModel
                {
                    Conversations = conversations
                });
        }

        var selectedConversationId =
            conversationId ??
            conversations[0].Id;

        var conversation =
            await dbContext.TrainerConversations
                .AsNoTracking()
                .Where(item =>
                    item.Id ==
                    selectedConversationId &&
                    item.MemberProfileId ==
                    memberProfileId.Value)
                .Select(item => new
                {
                    item.Id,

                    TrainerName =
                        item.Trainer.FirstName +
                        " " +
                        item.Trainer.LastName,

                    item.Trainer.Specialty
                })
                .FirstOrDefaultAsync();

        if (conversation is null)
        {
            return NotFound();
        }

        await messagingService.MarkAsReadByMemberAsync(
        userId,
        conversation.Id);

        var messages =
            await dbContext.TrainerMessages
                .AsNoTracking()
                .Where(message =>
                    message.TrainerConversationId ==
                    conversation.Id)
                .OrderBy(message =>
                    message.SentAtUtc)
                .Select(message =>
                    new MemberTrainerMessageListItemViewModel
                    {
                        Id =
                            message.Id,

                        Body =
                            message.Body,

                        SentAtUtc =
                            message.SentAtUtc,

                        IsMine =
                            message.SenderApplicationUserId ==
                            userId
                    })
                .ToListAsync();

        var canWrite =
            await messagingService
                .CanMemberWriteAsync(
                    userId,
                    conversation.Id);

        var activeConversation =
            new MemberConversationDetailViewModel
            {
                Id =
                    conversation.Id,

                TrainerName =
                    conversation.TrainerName,

                Specialty =
                    conversation.Specialty,

                CanWrite =
                    canWrite,

                Messages =
                    messages
            };

        return View(
            new MemberMessagesViewModel
            {
                Conversations =
                    conversations,

                ActiveConversation =
                    activeConversation
            });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Send(
        int conversationId,
        string? body)
    {
        var userId =
            User.FindFirstValue(
                ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Challenge();
        }

        var result =
            await messagingService.SendByMemberAsync(
                userId,
                conversationId,
                body);

        if (!result.Succeeded)
        {
            if (IsAjaxRequest())
            {
                return BadRequest(
                    new
                    {
                        succeeded = false,
                        message =
                            result.ErrorMessage
                    });
            }

            TempData["MessageError"] =
                result.ErrorMessage;

            return RedirectToAction(
                nameof(Index),
                new
                {
                    conversationId
                });
        }

        var realtimeMessage =
            new
            {
                messageId =
                    result.MessageId,

                conversationId =
                    result.ConversationId,

                body =
                    result.Body,

                sentAtUtc =
                    result.SentAtUtc,

                senderApplicationUserId =
                    result.SenderApplicationUserId
            };

        await hubContext.Clients
            .Group(
                TrainerChatHub
                    .GetConversationGroupName(
                        conversationId))
            .SendAsync(
                "MessageReceived",
                realtimeMessage);

        if (IsAjaxRequest())
        {
            return Ok(
                new
                {
                    succeeded = true,
                    message =
                        realtimeMessage
                });
        }

        return RedirectToAction(
            nameof(Index),
            new
            {
                conversationId
            });
    }

    private bool IsAjaxRequest()
    {
        return string.Equals(
            Request.Headers["X-Requested-With"],
            "XMLHttpRequest",
            StringComparison.OrdinalIgnoreCase);
    }
}