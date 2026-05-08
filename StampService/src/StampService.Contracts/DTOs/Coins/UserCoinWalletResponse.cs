namespace StampService.Contracts.DTOs.Coins;

public record UserCoinWalletResponse(
    Guid WalletId,
    Guid BrandId,
    string BrandName,
    int Value);
