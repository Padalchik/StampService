namespace StampService.Contracts.DTOs.Metrics;

public record RedeemMetricOptionResponse(
    Guid MetricDefinitionId,
    string MetricName,
    int CurrentBalance,
    int RequiredAmount,
    bool CanRedeem);
