namespace StampService.Contracts.DTOs.Wallet;

public record UserWalletBrandHistoryItemDetailsResponse(
    string SourceType,
    string SourceName,
    string TransactionType,
    int Amount,
    string AmountText,
    string? Comment,
    bool HasVisibleComment,
    Guid ActorUserId,
    DateTime CreatedAt);

