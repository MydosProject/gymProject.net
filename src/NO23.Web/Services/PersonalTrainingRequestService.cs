using Microsoft.EntityFrameworkCore;
using NO23.Web.Data;
using NO23.Web.Domain.Entities;
using NO23.Web.Domain.Enums;
using NO23.Web.ViewModels.Member;

namespace NO23.Web.Services;

public class PersonalTrainingRequestService(ApplicationDbContext dbContext)
{
    public static readonly IReadOnlyList<string> PreferredTimeWindows =
    [
        "09:00 - 12:00",
        "12:00 - 16:00",
        "16:00 - 20:00"
    ];

    public async Task<PersonalTrainingRequestResult> CreateAsync(
        string userId,
        PersonalTrainingRequestInputViewModel model)
    {
        if (!PreferredTimeWindows.Contains(model.PreferredTimeWindow))
        {
            return PersonalTrainingRequestResult.Fail("Geçerli bir saat aralığı seçmelisin.");
        }

        if (model.PreferredDate < DateOnly.FromDateTime(DateTime.Today))
        {
            return PersonalTrainingRequestResult.Fail("Geçmiş tarihli birebir talep oluşturulamaz.");
        }

        var profile = await dbContext.MemberProfiles
            .Include(member => member.MembershipPackage)
            .FirstOrDefaultAsync(member => member.ApplicationUserId == userId);

        if (profile is null)
        {
            return PersonalTrainingRequestResult.Fail("Üye profili bulunamadı.");
        }

        if (profile.IsSuspended)
        {
            return PersonalTrainingRequestResult.Fail(
                "Üyeliğiniz askıya alındığı için birebir antrenman talebi oluşturamazsınız.");
        }

        if (!profile.MembershipPackage.IncludesPersonalTrainingSupport)
        {
            return PersonalTrainingRequestResult.Fail(
                "Üyelik paketin birebir antrenman desteği içermiyor.");
        }

        var trainerExists = await dbContext.Trainers
            .AnyAsync(trainer => trainer.Id == model.TrainerId && trainer.IsActive);

        if (!trainerExists)
        {
            return PersonalTrainingRequestResult.Fail("Geçerli bir eğitmen seçmelisin.");
        }

        var hasActiveRequest = await dbContext.PersonalTrainingRequests
            .AnyAsync(request =>
                request.MemberProfileId == profile.Id &&
                request.TrainerId == model.TrainerId &&
                (
                    request.Status == PersonalTrainingRequestStatus.Pending ||
                    request.Status == PersonalTrainingRequestStatus.Scheduled
                ));

        if (hasActiveRequest)
        {
            return PersonalTrainingRequestResult.Fail(
                "Bu eğitmenle zaten devam eden bir birebir sürecin var. " +
                "Yeni talep oluşturmadan önce mevcut sürecin tamamlanmalı veya iptal edilmelidir.");
        }

        var conversationExists =
        await dbContext.TrainerConversations
            .AnyAsync(conversation =>
                conversation.MemberProfileId == profile.Id &&
                conversation.TrainerId == model.TrainerId);

        if (!conversationExists)
        {
            dbContext.TrainerConversations.Add(
                new TrainerConversation
                {
                    MemberProfileId = profile.Id,
                    TrainerId = model.TrainerId
                });
        }

        var request =
        new PersonalTrainingRequest
        {
            MemberProfileId = profile.Id,

            TrainerId = model.TrainerId,

            PreferredDate = model.PreferredDate,

            PreferredTimeWindow = model.PreferredTimeWindow,

            GoalNote = model.GoalNote?.Trim()
        };

        dbContext.PersonalTrainingRequests.Add(request);

        await dbContext.SaveChangesAsync();

        return PersonalTrainingRequestResult.Ok(request.Id);
    }

    public async Task<PersonalTrainingRequestResult> CancelByMemberAsync(
        string userId,
        int requestId)
    {
        var request = await dbContext.PersonalTrainingRequests
            .Include(item => item.MemberProfile)
            .FirstOrDefaultAsync(item =>
                item.Id == requestId &&
                item.MemberProfile.ApplicationUserId == userId);

        if (request is null)
        {
            return PersonalTrainingRequestResult.Fail("Birebir talep bulunamadı.");
        }

        var nowUtc = DateTime.UtcNow;

        if (request.Status == PersonalTrainingRequestStatus.Pending)
        {
            // Bekleyen talep iptal edilebilir.
        }
        else if (request.Status == PersonalTrainingRequestStatus.Scheduled)
        {
            if (request.ScheduledAtUtc is null)
            {
                return PersonalTrainingRequestResult.Fail(
                    "Planlanmış randevunun tarih bilgisi bulunamadı.");
            }

            if (request.ScheduledAtUtc <= nowUtc)
            {
                return PersonalTrainingRequestResult.Fail(
                    "Başlamış veya zamanı geçmiş birebir randevu iptal edilemez.");
            }
        }
        else
        {
            return PersonalTrainingRequestResult.Fail(
                "Bu birebir talep artık iptal edilemez.");
        }

            request.Status = PersonalTrainingRequestStatus.Cancelled;
            request.CancelledAtUtc = nowUtc;
            request.UpdatedAtUtc = nowUtc;

            await dbContext.SaveChangesAsync();
            return PersonalTrainingRequestResult.Ok(request.Id);
    }

    public async Task<PersonalTrainingRequestResult> UpdateByTrainerAsync(
    string trainerUserId,
    int requestId,
    PersonalTrainingRequestStatus status,
    DateTime? scheduledAtLocal,
    string? trainerNote)
    {
        if (status is not PersonalTrainingRequestStatus.Scheduled
            and not PersonalTrainingRequestStatus.Rejected)
        {
            return PersonalTrainingRequestResult.Fail(
                "Eğitmen yalnızca talebi planlayabilir veya reddedebilir.");
        }

        var request = await dbContext.PersonalTrainingRequests
            .Include(item => item.Trainer)
            .FirstOrDefaultAsync(item =>
                item.Id == requestId &&
                item.Trainer.ApplicationUserId == trainerUserId);

        if (request is null)
        {
            return PersonalTrainingRequestResult.Fail(
                "Birebir talep bulunamadı.");
        }

        if (request.Status != PersonalTrainingRequestStatus.Pending)
        {
            return PersonalTrainingRequestResult.Fail(
                "Yalnızca bekleyen birebir talepler yönetilebilir.");
        }

        var nowUtc = DateTime.UtcNow;

        if (status == PersonalTrainingRequestStatus.Scheduled)
        {
            if (!request.Trainer.IsActive)
            {
                return PersonalTrainingRequestResult.Fail(
                    "Pasif eğitmen için yeni birebir randevu planlanamaz.");
            }

            if (scheduledAtLocal is null)
            {
                return PersonalTrainingRequestResult.Fail(
                    "Randevuyu planlamak için kesin tarih ve saat girmelisin.");
            }

            var scheduledAtUtc = DateTime
                .SpecifyKind(
                    scheduledAtLocal.Value,
                    DateTimeKind.Local)
                .ToUniversalTime();

            if (scheduledAtUtc <= nowUtc)
            {
                return PersonalTrainingRequestResult.Fail(
                    "Kesin randevu tarihi geçmişte olamaz.");
            }

            request.ScheduledAtUtc = scheduledAtUtc;
        }
        else
        {
            request.ScheduledAtUtc = null;
        }

        request.Status = status;
        request.TrainerNote = trainerNote?.Trim();
        request.UpdatedAtUtc = nowUtc;

        await dbContext.SaveChangesAsync();

        return PersonalTrainingRequestResult.Ok(request.Id);
    }

    public async Task<PersonalTrainingRequestResult> UpdateByAdminAsync(
        int requestId,
        PersonalTrainingRequestStatus status,
        DateTime? scheduledAtLocal,
        string? adminNote)
    {
        var request = await dbContext.PersonalTrainingRequests
            .Include(item => item.Trainer)
            .FirstOrDefaultAsync(item => item.Id == requestId);

        if (request is null)
        {
            return PersonalTrainingRequestResult.Fail("Birebir talep bulunamadı.");
        }

        var nowUtc = DateTime.UtcNow;
        var scheduledAtUtc = scheduledAtLocal.HasValue
            ? DateTime.SpecifyKind(scheduledAtLocal.Value, DateTimeKind.Local).ToUniversalTime()
            : (DateTime?)null;

        switch (status)
        {
            case PersonalTrainingRequestStatus.Scheduled:
                if (request.Status is PersonalTrainingRequestStatus.Rejected or
                    PersonalTrainingRequestStatus.Cancelled or
                    PersonalTrainingRequestStatus.Completed)
                {
                    return PersonalTrainingRequestResult.Fail(
                        "Reddedilmiş, iptal edilmiş veya tamamlanmış talepler yeniden planlanamaz.");
                }

                if (!request.Trainer.IsActive && request.Status != PersonalTrainingRequestStatus.Scheduled)
                {
                    return PersonalTrainingRequestResult.Fail(
                        "Pasif eğitmen için yeni birebir randevu planlanamaz.");
                }

                if (scheduledAtUtc is null)
                {
                    return PersonalTrainingRequestResult.Fail("Kesin randevu tarihi zorunludur.");
                }

                if (scheduledAtUtc <= nowUtc)
                {
                    return PersonalTrainingRequestResult.Fail(
                        "Kesin randevu tarihi geçmişte olamaz.");
                }

                request.ScheduledAtUtc = scheduledAtUtc;
                request.CancelledAtUtc = null;
                request.CompletedAtUtc = null;
                break;

            case PersonalTrainingRequestStatus.Rejected:
                if (request.Status != PersonalTrainingRequestStatus.Pending)
                {
                    return PersonalTrainingRequestResult.Fail(
                        "Yalnızca bekleyen talepler reddedilebilir.");
                }

                request.ScheduledAtUtc = null;
                request.CancelledAtUtc = null;
                request.CompletedAtUtc = null;
                break;

            case PersonalTrainingRequestStatus.Cancelled:
                if (request.Status is PersonalTrainingRequestStatus.Rejected or
                    PersonalTrainingRequestStatus.Cancelled or
                    PersonalTrainingRequestStatus.Completed)
                {
                    return PersonalTrainingRequestResult.Fail(
                        "Bu talep durumu iptal işlemine uygun değil.");
                }

                request.CancelledAtUtc ??= nowUtc;
                request.CompletedAtUtc = null;
                break;

            case PersonalTrainingRequestStatus.Completed:
                if (request.Status != PersonalTrainingRequestStatus.Scheduled)
                {
                    return PersonalTrainingRequestResult.Fail(
                        "Yalnızca planlanmış birebir randevular tamamlanabilir.");
                }

                request.CompletedAtUtc = nowUtc;
                break;

            case PersonalTrainingRequestStatus.Pending:
                if (request.Status != PersonalTrainingRequestStatus.Pending)
                {
                    return PersonalTrainingRequestResult.Fail(
                        "İşleme alınmış talepler yeniden beklemeye alınamaz.");
                }

                request.ScheduledAtUtc = null;
                request.CancelledAtUtc = null;
                request.CompletedAtUtc = null;
                break;

            default:
                return PersonalTrainingRequestResult.Fail("Geçerli bir durum seçmelisin.");
        }

        request.Status = status;
        request.AdminNote = adminNote?.Trim();
        request.UpdatedAtUtc = nowUtc;

        await dbContext.SaveChangesAsync();
        return PersonalTrainingRequestResult.Ok(request.Id);
    }
}

public record PersonalTrainingRequestResult(
    bool Succeeded,
    string? ErrorMessage,
    int? RequestId = null)
{
    public static PersonalTrainingRequestResult Ok(
        int? requestId = null)
    {
        return new PersonalTrainingRequestResult(
            true,
            null,
            requestId);
    }

    public static PersonalTrainingRequestResult Fail(
        string message)
    {
        return new PersonalTrainingRequestResult(
            false,
            message);
    }
}
