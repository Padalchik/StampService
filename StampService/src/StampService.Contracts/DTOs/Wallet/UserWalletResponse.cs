namespace StampService.Contracts.DTOs.Wallet;

public record UserWalletResponse(
    Guid UserId,
    UserWalletRedemptionCodeResponse RedemptionCode,
    IReadOnlyCollection<UserWalletBrandOverviewResponse> Brands);
