namespace StampService.Contracts.DTOs.Metrics;

public record RedeemMetricRequest(
    string RedemptionCode,
    int Amount,
    string Comment);
