namespace StampService.Contracts.DTOs.Metrics;

public record BrandCustomerMetricBalanceResponse(
    Guid MetricDefinitionId,
    string MetricName,
    int Value,
    bool IsActive);
