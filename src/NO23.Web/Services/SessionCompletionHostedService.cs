using Microsoft.EntityFrameworkCore;
using NO23.Web.Data;
using NO23.Web.Domain.Enums;

namespace NO23.Web.Services;

/// <summary>Ders süresi dolduğunda planlanmış kayıtları tamamlandıya çevirir.
/// İptal, gelmedi ve ertelendi gibi manuel durumlara dokunmaz.</summary>
public sealed class SessionCompletionHostedService(IServiceScopeFactory scopeFactory, ILogger<SessionCompletionHostedService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var now = DateTime.UtcNow;
                var groupSessions = (await db.ClassSessions.Include(x => x.GroupClass)
                    .Where(x => x.Status == ClassSessionStatus.Scheduled && x.StartsAtUtc <= now)
                    .ToListAsync(stoppingToken))
                    .Where(x => x.StartsAtUtc.AddMinutes(Math.Max(1, x.GroupClass.DurationMinutes)) <= now)
                    .ToList();
                foreach (var session in groupSessions)
                {
                    session.Status = ClassSessionStatus.Completed;
                    session.UpdatedAtUtc = now;
                }
                var personalSessions = (await db.PersonalTrainingSessions
                    .Where(x => x.Status == PersonalTrainingSessionStatus.Scheduled && x.StartsAtUtc <= now)
                    .ToListAsync(stoppingToken))
                    .Where(x => x.StartsAtUtc.AddMinutes(Math.Max(1, x.DurationMinutes)) <= now)
                    .ToList();
                foreach (var session in personalSessions)
                {
                    session.Status = PersonalTrainingSessionStatus.Completed;
                    session.UpdatedAtUtc = now;
                }
                if (groupSessions.Count > 0 || personalSessions.Count > 0)
                    await db.SaveChangesAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception ex) { logger.LogError(ex, "Süre dolan dersler tamamlandıya çevrilemedi."); }
            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }
}
