namespace StampService.Contracts.DTOs.Wallet;

public record UserWalletOverviewResponse(
    Guid UserId,
    IReadOnlyCollection<UserWalletBrandOverviewResponse> Brands);
