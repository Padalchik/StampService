namespace StampService.Contracts.DTOs.Metrics;

public record BrandCustomerMetricBalanceResponse(
    Guid MetricDefinitionId,
    string MetricCode,
    string MetricName,
    int Value,
    bool IsActive);
