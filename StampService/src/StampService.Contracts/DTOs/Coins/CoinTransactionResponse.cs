namespace StampService.Contracts.DTOs.Coins;

public record CoinTransactionResponse(
    Guid TransactionId,
    Guid WalletId,
    Guid BrandId,
    Guid UserId,
    string TransactionType,
    int Amount,
    string Comment,
    DateTime CreatedAt);
