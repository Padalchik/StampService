namespace StampService.Contracts.DTOs.Wallet;

public record UserWalletRedemptionCodeResponse(
    string Code,
    DateTime ExpiresAtUtc);
