namespace StampService.Contracts.DTOs.Metrics;

public record CreateMetricRequest(
    string Name,
    int RedemptionAmount);
