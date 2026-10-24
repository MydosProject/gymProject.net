using System.Data;
using Microsoft.EntityFrameworkCore;
using NO23.Web.Domain.Entities;
using NO23.Web.Domain.Enums;

namespace NO23.Web.Services;

public partial class PersonalTrainingCalendarService
{
    public async Task<(bool Succeeded, string Message)> ReviewCancelledGroupCreditsAsync(int memberId, int expectedCount,
        int expectedBalance, bool confirmed, string actor, bool alreadyAdjusted)
    {
        if (!confirmed || expectedCount < 1 || string.IsNullOrWhiteSpace(actor)) return (false, "Ders geçmişini ve düzeltme yöntemini doğrulayın.");
        try
        {
            await using var transaction = dbContext.Database.IsRelational()
                ? await dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable) : null;
            await MemberCreditGuard.LockAsync(dbContext, memberId);
            var member = await dbContext.MemberProfiles.Include(x => x.MembershipPackage).FirstOrDefaultAsync(x => x.Id == memberId);
            if (member?.MembershipStartsAtUtc is null || MemberPackageEntitlement.HasUnlimitedClassAccess(member))
                return (false, "Paket dönemi ve hakları üye kaydından kontrol edin.");
            var reservations = await dbContext.ClassReservations.Where(x => x.MemberProfileId == memberId &&
                x.Status == ClassReservationStatus.Reserved && x.ClassSession.Status == ClassSessionStatus.Cancelled &&
                x.ClassSession.StartsAtUtc >= member.MembershipStartsAtUtc &&
                (!member.MembershipEndsAtUtc.HasValue || x.ClassSession.StartsAtUtc < member.MembershipEndsAtUtc)).ToListAsync();
            if (reservations.Count != expectedCount || member.RemainingClassCredits != expectedBalance)
                return (false, "Rezervasyonlar veya bakiye değişti. Sayfayı yenileyin.");
            foreach (var reservation in reservations)
            {
                reservation.Status = ClassReservationStatus.Cancelled;
                reservation.CancelledAtUtc = DateTime.UtcNow;
                reservation.CancellationReason = $"Yönetici hak kontrolü ({actor}): " + (alreadyAdjusted ? "Önceden iade edilmiş; bakiye korundu." : "İptal seansının hakkı bir kez iade edildi.");
                if (reservation.CancellationReason.Length > 300) reservation.CancellationReason = reservation.CancellationReason[..300];
            }
            if (!alreadyAdjusted) member.RemainingClassCredits += reservations.Count;
            member.UpdatedAtUtc = DateTime.UtcNow;
            await dbContext.SaveChangesAsync();
            if (transaction is not null) await transaction.CommitAsync();
            return (true, $"{reservations.Count} iptal rezervasyonu kontrol edilerek işlendi.");
        }
        catch (Exception exception) when (exception is DbUpdateException || exception.GetBaseException() is Npgsql.PostgresException { SqlState: "40001" or "40P01" })
        { return (false, "Haklar başka bir işlemle değişti. Sayfayı yenileyin."); }
    }

    // Explicit admin review only: old balances may already have been adjusted manually.
    // Comparing the displayed count and balance prevents applying a stale review.
    public async Task<(bool Succeeded, string Message)> ApplyReviewedCreditsAsync(
        int memberId, int expectedCount, int expectedBalance, bool confirmed, string actor, bool alreadyAdjusted = false)
    {
        if (!confirmed || expectedCount < 1 || string.IsNullOrWhiteSpace(actor))
            return (false, "Ders geçmişini, bakiyeyi ve seçilen düzeltme işlemini doğrulayın.");
        try
        {
            await using var transaction = dbContext.Database.IsRelational()
                ? await dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable) : null;
            await MemberCreditGuard.LockAsync(dbContext, memberId);
            var member = await dbContext.MemberProfiles.Include(m => m.MembershipPackage)
                .Include(m => m.ServicePackageVariant).FirstOrDefaultAsync(m => m.Id == memberId);
            if (member is null || member.MembershipStartsAtUtc is null || MemberPackageEntitlement.HasUnlimitedClassAccess(member))
                return (false, "Üyenin paket dönemi ve hakları üye kaydından kontrol edilmeli.");
            var lessons = await dbContext.PersonalTrainingSessions.Where(s => s.MemberProfileId == memberId &&
                (s.Status == PersonalTrainingSessionStatus.Completed || s.Status == PersonalTrainingSessionStatus.NoShow) && !s.CreditConsumed &&
                s.StartsAtUtc >= member.MembershipStartsAtUtc.Value &&
                (!member.MembershipEndsAtUtc.HasValue || s.StartsAtUtc < member.MembershipEndsAtUtc.Value)).ToListAsync();
            if (lessons.Count != expectedCount || member.RemainingClassCredits != expectedBalance)
                return (false, "Dersler veya bakiye değişti. Sayfayı yenileyip tekrar kontrol edin.");
            if (!alreadyAdjusted && member.RemainingClassCredits < lessons.Count)
                return (false, "Bakiye yetersiz; üyenin geçmiş hak hareketlerini kontrol edin.");
            var now = DateTime.UtcNow;
            foreach (var lesson in lessons)
            {
                lesson.CreditConsumed = true;
                lesson.UpdatedAtUtc = now;
                lesson.History.Add(new PersonalTrainingSessionHistory
                {
                    PreviousStatus = lesson.Status, NewStatus = lesson.Status,
                    PreviousStartsAtUtc = lesson.StartsAtUtc, NewStartsAtUtc = lesson.StartsAtUtc,
                    ChangedByUserId = actor, ChangedAtUtc = now,
                    Note = alreadyAdjusted
                        ? "Yönetici kontrolü: hak daha önce manuel düşülmüş; bakiye korunarak ders işlenmiş olarak işaretlendi."
                        : "Yönetici kontrolü: eski tamamlanmış dersin işlenmemiş hakkı bir kez düşüldü."
                });
            }
            if (!alreadyAdjusted) member.RemainingClassCredits -= lessons.Count;
            member.UpdatedAtUtc = now;
            await dbContext.SaveChangesAsync();
            if (transaction is not null) await transaction.CommitAsync();
            return (true, $"{lessons.Count} tamamlanmış dersin hakkı işlendi. Kalan hak: {member.RemainingClassCredits}.");
        }
        catch (Exception exception) when (exception is DbUpdateException ||
            exception.GetBaseException() is Npgsql.PostgresException { SqlState: "40001" or "40P01" })
        { return (false, "Haklar başka bir işlemle değişti. Sayfayı yenileyin."); }
    }
}
