namespace StampService.Contracts.DTOs.Wallet;

public record UserBrandRewardsResponse(
    Guid UserId,
    Guid BrandId,
    string BrandName,
    int CoinBalance,
    IReadOnlyCollection<UserBrandCoinProductRewardResponse> CoinProducts,
    IReadOnlyCollection<UserBrandMetricRewardResponse> Metrics);
