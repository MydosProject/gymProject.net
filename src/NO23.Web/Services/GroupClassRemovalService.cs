using Microsoft.EntityFrameworkCore;
using NO23.Web.Data;
using NO23.Web.Domain.Enums;

namespace NO23.Web.Services;

public class GroupClassRemovalService(ApplicationDbContext dbContext)
{
    public async Task<GroupClassRemovalResult> RemoveAsync(int groupClassId)
    {
        var group = await dbContext.GroupClasses
            .Include(item => item.Trainer)
            .FirstOrDefaultAsync(item => item.Id == groupClassId);
        if (group is null)
            return GroupClassRemovalResult.Fail("Grup dersi bulunamadı.");

        var hasSessions = await dbContext.ClassSessions.AnyAsync(item => item.GroupClassId == groupClassId);
        if (!hasSessions)
        {
            dbContext.GroupClasses.Remove(group);
            await dbContext.SaveChangesAsync();
            return new GroupClassRemovalResult(true, true, group.Name, 0, [], null, "Grup dersi silindi.");
        }

        var nowUtc = DateTime.UtcNow;
        var futureSessions = await dbContext.ClassSessions
            .Include(item => item.Reservations)
                .ThenInclude(item => item.MemberProfile)
                    .ThenInclude(item => item.MembershipPackage)
            .Where(item => item.GroupClassId == groupClassId &&
                item.Status == ClassSessionStatus.Scheduled && item.StartsAtUtc > nowUtc)
            .AsSplitQuery()
            .ToListAsync();

        var affectedMemberIds = new HashSet<string>();
        foreach (var session in futureSessions)
        {
            session.Status = ClassSessionStatus.Cancelled;
            session.UpdatedAtUtc = nowUtc;
            foreach (var reservation in session.Reservations.Where(item => item.Status == ClassReservationStatus.Reserved))
            {
                reservation.Status = ClassReservationStatus.Cancelled;
                reservation.CancelledAtUtc = nowUtc;
                reservation.CancellationReason = "Grup dersi yönetici tarafından kaldırıldı.";
                if (!MemberPackageEntitlement.HasUnlimitedClassAccess(reservation.MemberProfile))
                    reservation.MemberProfile.RemainingClassCredits++;
                reservation.MemberProfile.UpdatedAtUtc = nowUtc;
                affectedMemberIds.Add(reservation.MemberProfile.ApplicationUserId);
            }
        }

        group.IsActive = false;
        group.UpdatedAtUtc = nowUtc;
        await dbContext.SaveChangesAsync();
        return new GroupClassRemovalResult(
            true, false, group.Name, futureSessions.Count, affectedMemberIds.ToList(),
            group.Trainer.ApplicationUserId,
            futureSessions.Count == 0
                ? "Grup dersi geçmişi korunarak arşivlendi."
                : $"Grup dersi arşivlendi; {futureSessions.Count} gelecek seans iptal edildi ve katılımcı hakları iade edildi.");
    }
}

public record GroupClassRemovalResult(
    bool Succeeded,
    bool Deleted,
    string GroupClassName,
    int CancelledSessionCount,
    IReadOnlyList<string> AffectedMemberUserIds,
    string? TrainerUserId,
    string Message)
{
    public static GroupClassRemovalResult Fail(string message) =>
        new(false, false, string.Empty, 0, [], null, message);
}
