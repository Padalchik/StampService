namespace StampService.Contracts.DTOs.Wallet;

public record UserBrandWalletHistoryResponse(
    Guid UserId,
    Guid BrandId,
    string BrandName,
    bool IsMetricsEnabled,
    bool IsCoinsEnabled,
    int Skip,
    int Take,
    IReadOnlyCollection<UserBrandWalletHistoryItemResponse> Items);
