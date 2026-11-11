using SchoolAdmission.Api.Services.Interfaces;

namespace SchoolAdmission.Api.BackgroundServices;

public class HoldExpiryWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<HoldExpiryWorker> _logger;
    private readonly IConfiguration _configuration;

    public HoldExpiryWorker(
        IServiceScopeFactory scopeFactory,
        ILogger<HoldExpiryWorker> logger,
        IConfiguration configuration)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _configuration = configuration;
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        var intervalSeconds = _configuration.GetValue<int>(
            "Admission:HoldExpiryScanIntervalSeconds", 5);

        if (intervalSeconds < 1)
            intervalSeconds = 5;

        var interval = TimeSpan.FromSeconds(intervalSeconds);

        _logger.LogInformation(
            "Hold expiry worker started. Scan interval: {IntervalSeconds} seconds.",
            intervalSeconds);

        // Run once immediately when the application starts.
        await ExpireHoldsAsync(stoppingToken);

        using var timer = new PeriodicTimer(interval);

        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                await ExpireHoldsAsync(stoppingToken);
            }
        }
        catch (OperationCanceledException)
            when (stoppingToken.IsCancellationRequested)
        {
            // Normal application shutdown.
        }

        _logger.LogInformation("Hold expiry worker stopped.");
    }

    private async Task ExpireHoldsAsync(
        CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();

            var holdService = scope.ServiceProvider
                .GetRequiredService<IAdmissionHoldService>();

            var expiredCount = await holdService
                .ExpireOverdueHoldsAsync(cancellationToken);

            if (expiredCount > 0)
            {
                _logger.LogInformation(
                    "{ExpiredCount} overdue holds were expired.",
                    expiredCount);
            }
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "An error occurred while expiring overdue holds.");
        }
    }
}