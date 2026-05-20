namespace StampService.Contracts.DTOs.Wallet;

public record UserWalletResponse(
    Guid UserId,
    string CustomerCode,
    UserWalletRedemptionCodeResponse RedemptionCode,
    IReadOnlyCollection<UserWalletBrandOverviewResponse> Brands);
