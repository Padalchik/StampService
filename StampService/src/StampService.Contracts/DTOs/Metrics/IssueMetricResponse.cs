namespace StampService.Contracts.DTOs.Metrics;

public record IssueMetricResponse(
    Guid TransactionId,
    Guid BalanceId,
    Guid BrandId,
    Guid MetricDefinitionId,
    Guid UserId,
    int Amount,
    int BalanceValue,
    DateTime CreatedAt);
