namespace StampService.Application.Coins;

public record UserCoinWalletReadModel(
    Guid WalletId,
    Guid BrandId,
    string BrandName,
    int Value);
