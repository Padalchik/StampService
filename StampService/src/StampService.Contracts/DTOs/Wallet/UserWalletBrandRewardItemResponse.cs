namespace StampService.Contracts.DTOs.Wallet;

public record UserWalletBrandRewardItemResponse(
    Guid ItemId,
    string Name,
    string ProgressText,
    string StatusText,
    bool IsAvailable);

