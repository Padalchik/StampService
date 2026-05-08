namespace StampService.Application.Metrics;

public record UserMetricBalanceReadModel(
    Guid BalanceId,
    Guid BrandId,
    string BrandName,
    Guid MetricDefinitionId,
    string MetricCode,
    string MetricName,
    int RedemptionAmount,
    int Value);
