namespace StampService.Contracts.DTOs.Metrics;

public record MetricBalanceResponse(
    Guid? BalanceId,
    Guid BrandId,
    Guid MetricDefinitionId,
    Guid UserId,
    int Value);
