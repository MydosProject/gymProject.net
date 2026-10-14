using System.Data;
using Microsoft.EntityFrameworkCore;
using NO23.Web.Data;
using NO23.Web.Domain.Enums;

namespace NO23.Web.Services;

public sealed class SessionCompletionService(ApplicationDbContext db)
{
    public const string SystemActor = "system:session-completion";

    public async Task<int> CompleteDueAsync(DateTime now, CancellationToken cancellationToken = default)
    {
        // Manual outcomes use the same isolation level. Competing completions or
        // cancellations cannot commit a second debit or overwrite a member balance.
        await using var transaction = db.Database.IsRelational()
            ? await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken) : null;
        var groups = (await db.ClassSessions.Include(x => x.GroupClass)
            .Where(x => x.Status == ClassSessionStatus.Scheduled && x.StartsAtUtc <= now)
            .ToListAsync(cancellationToken))
            .Where(x => x.StartsAtUtc.AddMinutes(Math.Max(1, x.GroupClass.DurationMinutes)) <= now).ToList();
        var personal = (await db.PersonalTrainingSessions
            .Include(x => x.MemberProfile).ThenInclude(x => x.MembershipPackage)
            .Include(x => x.MemberProfile).ThenInclude(x => x.ServicePackageVariant)
            .Where(x => x.Status == PersonalTrainingSessionStatus.Scheduled && x.StartsAtUtc <= now)
            .ToListAsync(cancellationToken))
            .Where(x => x.StartsAtUtc.AddMinutes(Math.Max(1, x.DurationMinutes)) <= now).ToList();

        // Group credits were already reserved when participants joined the lesson.
        foreach (var session in groups)
        {
            session.Status = ClassSessionStatus.Completed;
            session.UpdatedAtUtc = now;
        }
        foreach (var session in personal)
            PersonalTrainingSessionFinalizer.Apply(session, PersonalTrainingSessionStatus.Completed,
                SystemActor, "Ders süresi dolduğu için otomatik tamamlandı.", now);

        if (groups.Count + personal.Count > 0) await db.SaveChangesAsync(cancellationToken);
        if (transaction is not null) await transaction.CommitAsync(cancellationToken);
        return groups.Count + personal.Count;
    }
}
