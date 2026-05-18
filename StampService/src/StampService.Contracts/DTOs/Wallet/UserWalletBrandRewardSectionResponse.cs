namespace StampService.Contracts.DTOs.Wallet;

public record UserWalletBrandRewardSectionResponse(
    string Kind,
    string Title,
    string? BalanceText,
    string EmptyText,
    IReadOnlyCollection<UserWalletBrandRewardItemResponse> Items);

