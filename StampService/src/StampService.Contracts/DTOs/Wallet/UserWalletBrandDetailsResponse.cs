namespace StampService.Contracts.DTOs.Wallet;

public record UserWalletBrandDetailsResponse(
    Guid UserId,
    Guid BrandId,
    string BrandName,
    IReadOnlyCollection<UserWalletBrandRewardSectionResponse> RewardSections,
    UserWalletBrandHistorySectionResponse History,
    string HintText);

