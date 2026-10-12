using Microsoft.EntityFrameworkCore;
using NO23.Web.Data;
using NO23.Web.Domain.Enums;

namespace NO23.Web.Services;

public class GroupClassRemovalService(ApplicationDbContext dbContext)
{
    public async Task<GroupClassRemovalResult> RemoveAsync(int groupClassId)
    {
        var group = await dbContext.GroupClasses.Include(item => item.Trainer)
            .FirstOrDefaultAsync(item => item.Id == groupClassId);
        if (group is null) return GroupClassRemovalResult.Fail("Grup dersi bulunamadı.");
        var sessions = await dbContext.ClassSessions.Include(item => item.Reservations)
            .ThenInclude(item => item.MemberProfile).ThenInclude(item => item.MembershipPackage)
            .Where(item => item.GroupClassId == groupClassId).AsSplitQuery().ToListAsync();
        var nowUtc = DateTime.UtcNow;
        var affected = sessions.Where(item => item.Status == ClassSessionStatus.Scheduled)
            .SelectMany(item => item.Reservations).Where(item => item.Status == ClassReservationStatus.Reserved)
            .Select(item => item.MemberProfile.ApplicationUserId).Distinct().ToList();
        foreach (var session in sessions.Where(item => item.Status == ClassSessionStatus.Scheduled))
            ClassReservationService.CancelSessionReservations(session, nowUtc, "Grup dersi yönetici tarafından silindi.");
        dbContext.ClassReservations.RemoveRange(sessions.SelectMany(item => item.Reservations));
        dbContext.ClassSessions.RemoveRange(sessions);
        dbContext.GroupClasses.Remove(group);
        await dbContext.SaveChangesAsync();
        return new(true, true, group.Name, sessions.Count(item => item.Status == ClassSessionStatus.Scheduled),
            affected, group.Trainer.ApplicationUserId, "Grup dersi ve seansları silindi; planlanmış rezervasyonların ders hakları iade edildi.");
    }

    public async Task<GroupClassRemovalResult> DeactivateAsync(int groupClassId)
    {
        var group = await dbContext.GroupClasses
            .Include(item => item.Trainer)
            .FirstOrDefaultAsync(item => item.Id == groupClassId);
        if (group is null)
            return GroupClassRemovalResult.Fail("Grup dersi bulunamadı.");

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
                affectedMemberIds.Add(reservation.MemberProfile.ApplicationUserId);
            ClassReservationService.CancelSessionReservations(session, nowUtc, "Grup dersi yönetici tarafından pasife alındı.");
        }

        group.IsActive = false;
        group.UpdatedAtUtc = nowUtc;
        await dbContext.SaveChangesAsync();
        return new GroupClassRemovalResult(
            true, false, group.Name, futureSessions.Count, affectedMemberIds.ToList(),
            group.Trainer.ApplicationUserId,
            futureSessions.Count == 0
                ? "Grup dersi pasife alındı; takvimde gösterilmeyecek."
                : $"Grup dersi pasife alındı; {futureSessions.Count} gelecek seans iptal edildi ve katılımcı hakları iade edildi.");
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
