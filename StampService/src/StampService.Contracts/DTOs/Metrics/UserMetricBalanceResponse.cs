namespace StampService.Contracts.DTOs.Metrics;

public record UserMetricBalanceResponse(
    Guid BalanceId,
    Guid BrandId,
    string BrandName,
    Guid MetricDefinitionId,
    string MetricName,
    int RedemptionAmount,
    int Value);
