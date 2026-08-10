using Microsoft.EntityFrameworkCore;
using NO23.Web.Data;
using NO23.Web.Domain.Entities;
using NO23.Web.Domain.Enums;

namespace NO23.Web.Services;

public class TrainerMessagingService(
    ApplicationDbContext dbContext)
{
    private const int MaximumMessageLength = 2000;

    public async Task<bool> CanAccessConversationAsync(
    string userId,
    int conversationId)
    {
        return await dbContext.TrainerConversations
            .AsNoTracking()
            .AnyAsync(conversation =>
                conversation.Id == conversationId &&
                (
                    conversation.MemberProfile
                        .ApplicationUserId == userId ||

                    conversation.Trainer
                        .ApplicationUserId == userId
                ));
    }

    public async Task<int> GetUnreadCountAsync(
    string userId)
    {
        return await dbContext.TrainerMessages
            .AsNoTracking()
            .CountAsync(message =>
                message.ReadAtUtc == null &&
                message.SenderApplicationUserId != userId &&
                (
                    message.TrainerConversation
                        .MemberProfile
                        .ApplicationUserId == userId ||

                    message.TrainerConversation
                        .Trainer
                        .ApplicationUserId == userId
                ));
    }

    public async Task<string?> GetOtherParticipantUserIdAsync(
        string userId,
        int conversationId)
    {
        var participants =
            await dbContext.TrainerConversations
                .AsNoTracking()
                .Where(conversation =>
                    conversation.Id == conversationId)
                .Select(conversation =>
                    new
                    {
                        MemberUserId =
                            conversation.MemberProfile
                                .ApplicationUserId,

                        TrainerUserId =
                            conversation.Trainer
                                .ApplicationUserId
                    })
                .SingleOrDefaultAsync();

        if (participants is null)
        {
            return null;
        }

        if (participants.MemberUserId == userId)
        {
            return participants.TrainerUserId;
        }

        if (participants.TrainerUserId == userId)
        {
            return participants.MemberUserId;
        }

        return null;
    }

    public async Task<bool> CanMemberWriteAsync(
        string userId,
        int conversationId)
    {
        var conversation =
            await dbContext.TrainerConversations
                .AsNoTracking()
                .Where(item =>
                    item.Id == conversationId &&
                    item.MemberProfile.ApplicationUserId ==
                    userId)
                .Select(item => new
                {
                    item.MemberProfileId,
                    item.TrainerId,
                    TrainerHasPanelAccount =
                        item.Trainer.ApplicationUserId != null
                })
                .FirstOrDefaultAsync();

        if (conversation is null ||
            !conversation.TrainerHasPanelAccount)
        {
            return false;
        }

        return await HasWritableProcessAsync(
            conversation.MemberProfileId,
            conversation.TrainerId);
    }

    public async Task<bool> CanTrainerWriteAsync(
    string userId,
    int conversationId)
    {
        var conversation =
            await dbContext.TrainerConversations
                .AsNoTracking()
                .Where(item =>
                    item.Id == conversationId &&
                    item.Trainer.ApplicationUserId == userId)
                .Select(item => new
                {
                    item.MemberProfileId,
                    item.TrainerId
                })
                .FirstOrDefaultAsync();

        if (conversation is null)
        {
            return false;
        }

        return await HasWritableProcessAsync(
            conversation.MemberProfileId,
            conversation.TrainerId);
    }

    public async Task<TrainerMessageSendResult> SendByMemberAsync(
            string userId,
            int conversationId,
            string? body)
    {
        var trimmedBody = body?.Trim();

        if (string.IsNullOrWhiteSpace(trimmedBody))
        {
            return TrainerMessageSendResult.Fail(
                "Mesaj boş olamaz.");
        }

        if (trimmedBody.Length > MaximumMessageLength)
        {
            return TrainerMessageSendResult.Fail(
                $"Mesaj en fazla {MaximumMessageLength} karakter olabilir.");
        }

        var conversation =
            await dbContext.TrainerConversations
                .Include(item => item.MemberProfile)
                .Include(item => item.Trainer)
                .FirstOrDefaultAsync(item =>
                    item.Id == conversationId &&
                    item.MemberProfile.ApplicationUserId ==
                    userId);

        if (conversation is null)
        {
            return TrainerMessageSendResult.Fail(
                "Konuşma bulunamadı.");
        }

        if (string.IsNullOrWhiteSpace(
            conversation.Trainer.ApplicationUserId))
        {
            return TrainerMessageSendResult.Fail(
                "Eğitmenin panel hesabı henüz aktif değil.");
        }

        var canWrite =
            await HasWritableProcessAsync(
                conversation.MemberProfileId,
                conversation.TrainerId);

        if (!canWrite)
        {
            return TrainerMessageSendResult.Fail(
                "Bu konuşma şu anda mesaj göndermeye açık değil.");
        }

        var nowUtc = DateTime.UtcNow;

        var message = new TrainerMessage
            {
                TrainerConversationId =
                    conversation.Id,

                SenderApplicationUserId =
                    userId,

                Body =
                    trimmedBody,

                SentAtUtc =
                    nowUtc
            };

        dbContext.TrainerMessages.Add(message);

        conversation.LastMessageAtUtc = nowUtc;

        await dbContext.SaveChangesAsync();

        return TrainerMessageSendResult.Ok(message);
    }
    
    public async Task<bool> MarkAsReadByMemberAsync(
    string userId,
    int conversationId)
    {
        var result =
            await MarkConversationAsReadAsync(
                userId,
                conversationId);

        return result.Succeeded;
    }


    public async Task<TrainerMessageSendResult> SendByTrainerAsync(
        string userId,
        int conversationId,
        string? body)
    {
        var trimmedBody = body?.Trim();

        if (string.IsNullOrWhiteSpace(trimmedBody))
        {
            return TrainerMessageSendResult.Fail(
                "Mesaj boş olamaz.");
        }

        if (trimmedBody.Length > MaximumMessageLength)
        {
            return TrainerMessageSendResult.Fail(
                $"Mesaj en fazla {MaximumMessageLength} karakter olabilir.");
        }

        var conversation =
            await dbContext.TrainerConversations
                .FirstOrDefaultAsync(item =>
                    item.Id == conversationId &&
                    item.Trainer.ApplicationUserId == userId);

        if (conversation is null)
        {
            return TrainerMessageSendResult.Fail(
                "Konuşma bulunamadı.");
        }

        var canWrite =
           await HasWritableProcessAsync(
            conversation.MemberProfileId,
            conversation.TrainerId);

        if (!canWrite)
        {
            return TrainerMessageSendResult.Fail(
                "Bu konuşma şu anda mesaj göndermeye açık değil.");
        }

        var nowUtc = DateTime.UtcNow;

        var message = new TrainerMessage
            {
                TrainerConversationId =
                    conversation.Id,

                SenderApplicationUserId =
                    userId,

                Body =
                    trimmedBody,

                SentAtUtc =
                    nowUtc
            };

        dbContext.TrainerMessages.Add(message);

        conversation.LastMessageAtUtc = nowUtc;

        await dbContext.SaveChangesAsync();

        return TrainerMessageSendResult.Ok(message);
    }

    public async Task<TrainerMessageReadResult> MarkConversationAsReadAsync(
    string userId,
    int conversationId)
    {
        var canAccess =
            await CanAccessConversationAsync(
                userId,
                conversationId);

        if (!canAccess)
        {
            return TrainerMessageReadResult.Fail(
                "Konuşmaya erişemezsiniz.");
        }

        var unreadMessages =
            await dbContext.TrainerMessages
                .Where(message =>
                    message.TrainerConversationId ==
                    conversationId &&
                    message.SenderApplicationUserId !=
                    userId &&
                    message.ReadAtUtc == null)
                .ToListAsync();

        if (unreadMessages.Count == 0)
        {
            return TrainerMessageReadResult.Ok(
                [],
                null);
        }

        var nowUtc = DateTime.UtcNow;

        foreach (var message in unreadMessages)
        {
            message.ReadAtUtc = nowUtc;
        }

        await dbContext.SaveChangesAsync();

        return TrainerMessageReadResult.Ok(
            unreadMessages
                .Select(message => message.Id)
                .ToArray(),
            nowUtc);
    }

    public async Task<bool> MarkAsReadByTrainerAsync(
        string userId,
        int conversationId)
    {
        var result =
            await MarkConversationAsReadAsync(
                userId,
                conversationId);

        return result.Succeeded;
    }


    private async Task<bool> HasWritableProcessAsync(
        int memberProfileId,
        int trainerId)
    {
        var completedThresholdUtc =
            DateTime.UtcNow.AddHours(-48);

        return await dbContext.PersonalTrainingRequests
            .AsNoTracking()
            .AnyAsync(request =>
                request.MemberProfileId ==
                memberProfileId &&
                request.TrainerId ==
                trainerId &&
                (
                    request.Status ==
                    PersonalTrainingRequestStatus.Pending ||

                    request.Status ==
                    PersonalTrainingRequestStatus.Scheduled ||

                    (
                        request.Status ==
                        PersonalTrainingRequestStatus.Completed &&
                        request.CompletedAtUtc != null &&
                        request.CompletedAtUtc >=
                        completedThresholdUtc
                    )
                ));
    }


    public record TrainerMessageSendResult(
    bool Succeeded,
    string? ErrorMessage,
    int? MessageId = null,
    int? ConversationId = null,
    string? Body = null,
    DateTime? SentAtUtc = null,
    string? SenderApplicationUserId = null)
    {
        public static TrainerMessageSendResult Ok(
            TrainerMessage message)
        {
            return new TrainerMessageSendResult(
                true,
                null,
                message.Id,
                message.TrainerConversationId,
                message.Body,
                message.SentAtUtc,
                message.SenderApplicationUserId);
        }

        public static TrainerMessageSendResult Fail(
            string message)
        {
            return new TrainerMessageSendResult(
                false,
                message);
        }
    }

    public record TrainerMessageReadResult(
        bool Succeeded,
        string? ErrorMessage,
        IReadOnlyList<int> MessageIds,
        DateTime? ReadAtUtc)
    {
        public static TrainerMessageReadResult Ok(
            IReadOnlyList<int> messageIds,
            DateTime? readAtUtc)
        {
            return new TrainerMessageReadResult(
                true,
                null,
                messageIds,
                readAtUtc);
        }

        public static TrainerMessageReadResult Fail(
            string message)
        {
            return new TrainerMessageReadResult(
                false,
                message,
                [],
                null);
        }
    }

}
