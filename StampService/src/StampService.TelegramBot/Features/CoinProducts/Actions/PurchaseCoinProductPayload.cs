namespace StampService.TelegramBot.Features.CoinProducts.Actions;

public record PurchaseCoinProductPayload(
    Guid ProductId,
    bool CanPurchase);
