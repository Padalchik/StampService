namespace StampService.Contracts.DTOs.Brands;

public record BrandWelcomeRewardSettingsResponse(
    bool IsEnabled,
    IReadOnlyCollection<BrandWelcomeMetricRewardResponse> Metrics,
    int CoinsAmount,
    string Comment);
