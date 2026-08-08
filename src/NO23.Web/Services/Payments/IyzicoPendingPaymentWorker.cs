namespace NO23.Web.Services.Payments;

public sealed class IyzicoPendingPaymentWorker(
    IServiceScopeFactory scopeFactory,
    ILogger<IyzicoPendingPaymentWorker> logger)
    : BackgroundService
{
    private static readonly TimeSpan Interval =
        TimeSpan.FromMinutes(5);

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        logger.LogInformation(
            "Iyzico pending payment worker started.");

        // Uygulama açılır açılmaz ilk kontrolü yap.
        await ProcessOnceAsync(stoppingToken);

        using var timer =
            new PeriodicTimer(Interval);

        try
        {
            while (await timer.WaitForNextTickAsync(
                       stoppingToken))
            {
                await ProcessOnceAsync(
                    stoppingToken);
            }
        }
        catch (OperationCanceledException)
            when (stoppingToken.IsCancellationRequested)
        {
            // Uygulama kapanırken normal davranış.
        }

        logger.LogInformation(
            "Iyzico pending payment worker stopped.");
    }

    private async Task ProcessOnceAsync(
        CancellationToken cancellationToken)
    {
        try
        {
            using var scope =
                scopeFactory.CreateScope();

            var pendingPaymentService =
                scope.ServiceProvider
                    .GetRequiredService<IyzicoPendingPaymentService>();

            var processedCount =
                await pendingPaymentService
                    .ProcessExpiredPaymentsAsync(
                        cancellationToken);

            if (processedCount > 0)
            {
                logger.LogInformation(
                    "Processed {ProcessedCount} expired iyzico payment(s).",
                    processedCount);
            }
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
            // Uygulama kapanıyor.
        }
        catch (Exception exception)
        {
            // Bir worker hatası uygulamayı düşürmemeli.
            // Bir sonraki turda yeniden denenecek.
            logger.LogError(
                exception,
                "An error occurred while processing expired iyzico payments.");
        }
    }
}