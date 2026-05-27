using StampService.Contracts.DTOs.Coins;
using StampService.Contracts.DTOs.Metrics;

namespace StampService.Application.CustomerNotifications;

public interface ICustomerNotificationService
{
    Task NotifyCoinsIssuedAsync(
        CoinOperationResponse operation,
        CancellationToken cancellationToken);

    Task NotifyMetricIssuedAsync(
        IssueMetricResponse operation,
        CancellationToken cancellationToken);
}
