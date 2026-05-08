namespace StampService.Contracts.DTOs.Metrics;

public record RedeemMetricOptionResponse(
    Guid MetricDefinitionId,
    string MetricName,
    string MetricCode,
    int CurrentBalance,
    int RequiredAmount,
    bool CanRedeem);
