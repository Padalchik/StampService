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

    Task NotifyCoinsRedeemedAsync(
        CoinOperationResponse operation,
        string comment,
        CancellationToken cancellationToken);

    Task NotifyCoinProductPurchasedAsync(
        CoinOperationResponse operation,
        string productName,
        CancellationToken cancellationToken);

    Task NotifyMetricRedeemedAsync(
        RedeemMetricResponse operation,
        CancellationToken cancellationToken);
}
