namespace StampService.Contracts.DTOs.Users;

public record CreateRedemptionCodeResponse(
    string Code,
    DateTime ExpiresAtUtc);
