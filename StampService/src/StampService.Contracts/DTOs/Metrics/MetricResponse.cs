namespace StampService.Contracts.DTOs.Metrics;

public record MetricResponse(
    Guid Id,
    Guid BrandId,
    string Code,
    string Name,
    int RedemptionAmount,
    bool IsActive,
    DateTime CreatedAt);
