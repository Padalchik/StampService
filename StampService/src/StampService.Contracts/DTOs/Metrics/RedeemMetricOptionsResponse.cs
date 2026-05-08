namespace StampService.Contracts.DTOs.Metrics;

public record RedeemMetricOptionsResponse(
    Guid CustomerUserId,
    string CustomerName,
    string RedemptionCode,
    IReadOnlyCollection<RedeemMetricOptionResponse> Metrics);
