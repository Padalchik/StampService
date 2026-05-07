namespace StampService.TelegramBot.Features.RedeemMetric.Actions;

public record SelectRedeemMetricPayload(
    Guid MetricDefinitionId,
    string MetricName);
