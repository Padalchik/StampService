namespace StampService.Contracts.DTOs.Coins;

public record CoinOperationResponse(
    Guid TransactionId,
    Guid WalletId,
    Guid BrandId,
    Guid UserId,
    string UserName,
    string CustomerCode,
    string TransactionType,
    int Amount,
    int BalanceValue,
    DateTime CreatedAt);
