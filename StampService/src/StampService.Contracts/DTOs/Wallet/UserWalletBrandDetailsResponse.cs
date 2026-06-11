namespace StampService.Contracts.DTOs.Wallet;

public record UserWalletBrandDetailsResponse(
    Guid UserId,
    Guid BrandId,
    string BrandName,
    bool IsMetricsEnabled,
    bool IsCoinsEnabled,
    bool IsCoinProductRedemptionEnabled,
    int CoinBalance,
    IReadOnlyCollection<UserWalletBrandRewardSectionResponse> RewardSections,
    UserWalletBrandHistorySectionResponse History,
    string HintText);
