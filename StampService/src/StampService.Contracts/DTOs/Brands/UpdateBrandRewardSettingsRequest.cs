namespace StampService.Contracts.DTOs.Brands;

public record UpdateBrandRewardSettingsRequest(
    bool IsMetricsEnabled,
    bool IsCoinsEnabled,
    bool IsCoinProductRedemptionEnabled,
    bool IsManualCoinRedemptionEnabled,
    BrandWelcomeRewardSettingsRequest? WelcomeRewards);
