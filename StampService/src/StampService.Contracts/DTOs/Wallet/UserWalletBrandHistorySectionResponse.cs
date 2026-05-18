namespace StampService.Contracts.DTOs.Wallet;

public record UserWalletBrandHistorySectionResponse(
    string Title,
    string EmptyText,
    IReadOnlyCollection<UserWalletBrandHistoryGroupResponse> Groups);

