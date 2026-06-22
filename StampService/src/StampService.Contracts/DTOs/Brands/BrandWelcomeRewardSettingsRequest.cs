namespace StampService.Contracts.DTOs.Brands;

public record BrandWelcomeRewardSettingsRequest(
    bool IsEnabled,
    IReadOnlyCollection<BrandWelcomeMetricRewardRequest>? Metrics,
    int CoinsAmount,
    string? Comment);
