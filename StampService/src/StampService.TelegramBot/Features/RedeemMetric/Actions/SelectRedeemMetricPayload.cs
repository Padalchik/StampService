namespace StampService.TelegramBot.Features.RedeemMetric.Actions;

public record SelectRedeemMetricPayload(
    Guid MetricDefinitionId,
    string MetricName,
    int RedemptionAmount,
    int CurrentBalance,
    bool CanRedeem);
