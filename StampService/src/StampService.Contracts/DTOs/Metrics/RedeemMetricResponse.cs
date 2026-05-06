namespace StampService.Contracts.DTOs.Metrics;

public record RedeemMetricResponse(
    Guid TransactionId,
    Guid BalanceId,
    Guid BrandId,
    Guid MetricDefinitionId,
    Guid UserId,
    string TransactionType,
    int Amount,
    int BalanceValue,
    DateTime CreatedAt);
