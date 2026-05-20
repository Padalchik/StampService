namespace StampService.Contracts.DTOs.Wallet;

public record UserWalletBrandHistoryGroupResponse(
    string Kind,
    string Title,
    string EmptyText,
    IReadOnlyCollection<UserWalletBrandHistoryItemDetailsResponse> Items);

