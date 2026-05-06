namespace StampService.Contracts.DTOs.Metrics;

public record MetricTransactionResponse(
    Guid Id,
    Guid BalanceId,
    Guid MetricDefinitionId,
    Guid UserId,
    int Amount,
    string Comment,
    DateTime CreatedAt);
