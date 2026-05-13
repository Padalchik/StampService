namespace StampService.TelegramBot.Features.CoinProducts.Actions;

public record SelectPurchaseCoinProductPayload(
    Guid ProductId,
    string ProductName,
    int Price,
    int CurrentBalance,
    bool CanPurchase);
