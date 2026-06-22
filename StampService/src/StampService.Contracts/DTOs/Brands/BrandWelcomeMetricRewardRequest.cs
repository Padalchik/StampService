namespace StampService.Contracts.DTOs.Brands;

public record BrandWelcomeMetricRewardRequest(
    Guid MetricDefinitionId,
    int Amount);
