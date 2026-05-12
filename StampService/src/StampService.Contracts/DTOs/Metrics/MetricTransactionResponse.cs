namespace StampService.Contracts.DTOs.Metrics;

public record MetricTransactionResponse(
    Guid Id,
    Guid BalanceId,
    Guid MetricDefinitionId,
    Guid UserId,
    string TransactionType,
    int Amount,
    string Comment,
    Guid ActorUserId,
    DateTime CreatedAt);
