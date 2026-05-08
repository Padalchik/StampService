namespace StampService.Contracts.DTOs.Metrics;

public record RedeemMetricRequest(
    string RedemptionCode,
    string Comment);
