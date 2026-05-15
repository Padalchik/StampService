using StampService.Contracts.DTOs.Coins;
using StampService.Contracts.DTOs.Metrics;

namespace StampService.TelegramBot.Common.Notifications;

public interface ICustomerNotificationService
{
    Task NotifyCoinsIssuedAsync(
        CoinOperationResponse operation,
        string brandName,
        CancellationToken cancellationToken);

    Task NotifyCoinProductPurchasedAsync(
        CoinOperationResponse operation,
        string brandName,
        string productName,
        CancellationToken cancellationToken);

    Task NotifyCoinsRedeemedAsync(
        CoinOperationResponse operation,
        string brandName,
        string comment,
        CancellationToken cancellationToken);

    Task NotifyMetricIssuedAsync(
        IssueMetricResponse operation,
        string brandName,
        string metricName,
        CancellationToken cancellationToken);

    Task NotifyMetricRedeemedAsync(
        RedeemMetricResponse operation,
        string brandName,
        string metricName,
        CancellationToken cancellationToken);
}
