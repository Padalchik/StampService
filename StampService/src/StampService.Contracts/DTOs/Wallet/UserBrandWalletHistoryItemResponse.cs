namespace StampService.Contracts.DTOs.Wallet;

public record UserBrandWalletHistoryItemResponse(
    string SourceType,
    string SourceName,
    string TransactionType,
    int Amount,
    string? Comment,
    DateTime CreatedAt);
