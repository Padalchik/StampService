namespace StampService.Contracts.DTOs.Metrics;

public record UpdateMetricRequest(
    string Name,
    int RedemptionAmount);
