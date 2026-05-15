namespace StampService.Contracts.DTOs.Wallet;

public record UserBrandRewardsResponse(
    Guid UserId,
    Guid BrandId,
    string BrandName,
    bool IsMetricsEnabled,
    bool IsCoinsEnabled,
    bool IsCoinProductRedemptionEnabled,
    bool IsManualCoinRedemptionEnabled,
    int CoinBalance,
    IReadOnlyCollection<UserBrandCoinProductRewardResponse> CoinProducts,
    IReadOnlyCollection<UserBrandMetricRewardResponse> Metrics);
