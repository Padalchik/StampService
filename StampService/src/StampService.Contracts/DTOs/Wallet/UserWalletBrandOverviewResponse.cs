namespace StampService.Contracts.DTOs.Wallet;

public record UserWalletBrandOverviewResponse(
    Guid BrandId,
    string BrandName,
    int CoinBalance,
    IReadOnlyCollection<UserBrandCoinProductRewardResponse> AvailableCoinProducts,
    IReadOnlyCollection<UserBrandMetricRewardResponse> AvailableMetrics);
