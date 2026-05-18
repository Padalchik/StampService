namespace StampService.Contracts.DTOs.Profile;

public record RequestTelegramLinkResponse(
    string TelegramLinkUrl,
    DateTime ExpiresAtUtc);

