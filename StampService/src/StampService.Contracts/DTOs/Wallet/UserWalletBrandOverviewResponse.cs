namespace StampService.Contracts.DTOs.Wallet;

public record UserWalletBrandOverviewResponse(
    Guid BrandId,
    string BrandName,
    bool IsMetricsEnabled,
    bool IsCoinsEnabled,
    int CoinBalance,
    IReadOnlyCollection<UserBrandCoinProductRewardResponse> AvailableCoinProducts,
    IReadOnlyCollection<UserBrandMetricRewardResponse> AvailableMetrics);
