using StampService.Contracts.DTOs.Coins;
using StampService.Contracts.DTOs.Metrics;

namespace StampService.Application.CustomerNotifications;

public sealed class NullCustomerNotificationService : ICustomerNotificationService
{
    public static NullCustomerNotificationService Instance { get; } = new();

    private NullCustomerNotificationService()
    {
    }

    public Task NotifyCoinsIssuedAsync(
        CoinOperationResponse operation,
        CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public Task NotifyMetricIssuedAsync(
        IssueMetricResponse operation,
        CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
