namespace StampService.Contracts.DTOs.Metrics;

public record RedeemMetricRequest(
    Guid UserId,
    int Amount,
    string Comment);
