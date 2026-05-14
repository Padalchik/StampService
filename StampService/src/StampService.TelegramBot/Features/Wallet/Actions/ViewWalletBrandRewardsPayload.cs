namespace StampService.TelegramBot.Features.Wallet.Actions;

public record ViewWalletBrandRewardsPayload(
    Guid BrandId,
    string BrandName);
