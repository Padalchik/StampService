namespace StampService.TelegramBot.Features.Wallet.Actions;

public record ViewWalletBrandHistoryPayload(
    Guid BrandId,
    string BrandName);
