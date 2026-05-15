using StampService.Application.CustomerNotifications;

namespace StampService.TelegramBot.Common.Notifications;

public sealed class CustomerRewardDigestHostedService : BackgroundService
{
    private static readonly TimeSpan SettingsRefreshDelay = TimeSpan.FromMinutes(1);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<CustomerRewardDigestHostedService> _logger;
    private DateTimeOffset? _lastScanAtUtc;

    public CustomerRewardDigestHostedService(
        IServiceScopeFactory scopeFactory,
        TimeProvider timeProvider,
        ILogger<CustomerRewardDigestHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var delay = await RunDueScanAsync(stoppingToken);

            try
            {
                await Task.Delay(delay, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    private async Task<TimeSpan> RunDueScanAsync(CancellationToken cancellationToken)
    {
        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var settingsRepository = scope.ServiceProvider.GetRequiredService<IRewardDigestSettingsRepository>();
            var settings = await settingsRepository.GetOrCreateAsync(cancellationToken);
            var nowUtc = _timeProvider.GetUtcNow();
            var scanInterval = TimeSpan.FromMinutes(settings.ScanIntervalMinutes);

            if (_lastScanAtUtc is not null)
            {
                var remaining = scanInterval - (nowUtc - _lastScanAtUtc.Value);
                if (remaining > TimeSpan.Zero)
                    return Min(remaining, SettingsRefreshDelay);
            }

            var sender = scope.ServiceProvider.GetRequiredService<CustomerRewardDigestSender>();
            await sender.SendDueDigestsAsync(cancellationToken);
            _lastScanAtUtc = nowUtc;

            return Min(scanInterval, SettingsRefreshDelay);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Reward digest scan failed");
            return SettingsRefreshDelay;
        }
    }

    private static TimeSpan Min(TimeSpan left, TimeSpan right)
    {
        return left <= right ? left : right;
    }
}
