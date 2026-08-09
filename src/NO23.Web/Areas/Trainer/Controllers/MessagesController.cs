using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NO23.Web.Data;
using NO23.Web.Data.Seed;
using NO23.Web.Services;
using NO23.Web.ViewModels.TrainerPanel;
using Microsoft.AspNetCore.SignalR;
using NO23.Web.Hubs;

namespace NO23.Web.Areas.Trainer.Controllers;

[Area("Trainer")]
[Authorize(Roles = ApplicationRoles.Trainer)]
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

        var trainerId =
            await dbContext.Trainers
                .AsNoTracking()
                .Where(trainer =>
                    trainer.ApplicationUserId == userId)
                .Select(trainer =>
                    (int?)trainer.Id)
                .FirstOrDefaultAsync();

        if (trainerId is null)
        {
            return Forbid();
        }

        var conversationRows =
            await dbContext.TrainerConversations
                .AsNoTracking()
                .Where(conversation =>
                    conversation.TrainerId ==
                    trainerId.Value)
                .OrderByDescending(conversation =>
                    conversation.LastMessageAtUtc ??
                    conversation.CreatedAtUtc)
                .Select(conversation => new
                {
                    conversation.Id,

                    MemberFirstName =
                        conversation.MemberProfile
                            .ApplicationUser.FirstName,

                    MemberLastName =
                        conversation.MemberProfile
                            .ApplicationUser.LastName,

                    MemberEmail =
                        conversation.MemberProfile
                            .ApplicationUser.Email,

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

        var conversations =
            conversationRows
                .Select(conversation =>
                {
                    var memberEmail =
                        conversation.MemberEmail ??
                        string.Empty;

                    var memberName =
                        $"{conversation.MemberFirstName} " +
                        $"{conversation.MemberLastName}";

                    memberName = memberName.Trim();

                    return new
                        TrainerConversationListItemViewModel
                        {
                            Id =
                                conversation.Id,

                            MemberName =
                                string.IsNullOrWhiteSpace(
                                    memberName)
                                    ? memberEmail
                                    : memberName,

                            MemberEmail =
                                memberEmail,

                            LastMessage =
                                conversation.LastMessage,

                            LastActivityAtUtc =
                                conversation.LastActivityAtUtc
                        };
                })
                .ToList();

        if (conversations.Count == 0)
        {
            return View(
                new TrainerMessagesViewModel
                {
                    Conversations =
                        conversations
                });
        }

        var selectedConversationId =
            conversationId ??
            conversations[0].Id;

        var conversationRow =
            await dbContext.TrainerConversations
                .AsNoTracking()
                .Where(conversation =>
                    conversation.Id ==
                    selectedConversationId &&
                    conversation.TrainerId ==
                    trainerId.Value)
                .Select(conversation => new
                {
                    conversation.Id,

                    MemberFirstName =
                        conversation.MemberProfile
                            .ApplicationUser.FirstName,

                    MemberLastName =
                        conversation.MemberProfile
                            .ApplicationUser.LastName,

                    MemberEmail =
                        conversation.MemberProfile
                            .ApplicationUser.Email
                })
                .FirstOrDefaultAsync();

        if (conversationRow is null)
        {
            return NotFound();
        }

        await messagingService.MarkAsReadByTrainerAsync(
        userId,
        conversationRow.Id);

        var memberEmail =
            conversationRow.MemberEmail ??
            string.Empty;

        var memberName =
            $"{conversationRow.MemberFirstName} " +
            $"{conversationRow.MemberLastName}";

        memberName = memberName.Trim();

        var messages =
            await dbContext.TrainerMessages
                .AsNoTracking()
                .Where(message =>
                    message.TrainerConversationId ==
                    conversationRow.Id)
                .OrderBy(message =>
                    message.SentAtUtc)
                .Select(message =>
                    new TrainerMessageListItemViewModel
                    {
                        Id =
                            message.Id,

                        Body =
                            message.Body,

                        SentAtUtc =
                            message.SentAtUtc,

                        IsMine =
                            message
                                .SenderApplicationUserId ==
                            userId
                    })
                .ToListAsync();

        var canWrite =
            await messagingService
                .CanTrainerWriteAsync(
                    userId,
                    conversationRow.Id);

        var activeConversation =
            new TrainerConversationDetailViewModel
            {
                Id =
                    conversationRow.Id,

                MemberName =
                    string.IsNullOrWhiteSpace(memberName)
                        ? memberEmail
                        : memberName,

                MemberEmail =
                    memberEmail,

                CanWrite =
                    canWrite,

                Messages =
                    messages
            };

        return View(
            new TrainerMessagesViewModel
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
            await messagingService
                .SendByTrainerAsync(
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

            TempData["TrainerMessageError"] =
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