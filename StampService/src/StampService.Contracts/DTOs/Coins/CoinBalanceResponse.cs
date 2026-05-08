namespace StampService.Contracts.DTOs.Coins;

public record CoinBalanceResponse(
    Guid? WalletId,
    Guid BrandId,
    Guid UserId,
    string UserName,
    string CustomerCode,
    int Value);
