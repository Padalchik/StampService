namespace StampService.Contracts.DTOs.Metrics;

public record UserMetricBalanceResponse(
    Guid BalanceId,
    Guid BrandId,
    string BrandName,
    Guid MetricDefinitionId,
    string MetricCode,
    string MetricName,
    int Value);
