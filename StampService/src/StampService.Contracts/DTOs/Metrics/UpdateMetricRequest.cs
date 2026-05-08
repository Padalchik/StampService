namespace StampService.Contracts.DTOs.Metrics;

public record UpdateMetricRequest(
    string Code,
    string Name,
    int RedemptionAmount);
