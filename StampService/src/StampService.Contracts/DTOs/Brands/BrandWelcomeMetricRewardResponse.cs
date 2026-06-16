namespace StampService.Contracts.DTOs.Brands;

public record BrandWelcomeMetricRewardResponse(
    Guid MetricDefinitionId,
    int Amount);
