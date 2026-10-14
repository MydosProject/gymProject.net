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
                await scope.ServiceProvider.GetRequiredService<SessionCompletionService>()
                    .CompleteDueAsync(DateTime.UtcNow, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception ex) when (ex.GetBaseException() is Npgsql.PostgresException { SqlState: "40001" or "40P01" })
            { logger.LogWarning("Dersler başka bir işlemle değişti; otomatik tamamlama sonraki çalışmada tekrar denenecek."); }
            catch (Exception ex) { logger.LogError(ex, "Süre dolan dersler tamamlandıya çevrilemedi."); }
            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }
}
