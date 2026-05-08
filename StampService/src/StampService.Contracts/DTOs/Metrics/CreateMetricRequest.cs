namespace StampService.Contracts.DTOs.Metrics;

public record CreateMetricRequest(
    string Code,
    string Name,
    int RedemptionAmount);
