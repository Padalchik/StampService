namespace StampService.Contracts.DTOs.Metrics;

public record MetricTransactionsResponse(
    Guid BrandId,
    Guid MetricDefinitionId,
    Guid UserId,
    int Skip,
    int Take,
    IReadOnlyCollection<MetricTransactionResponse> Items);
