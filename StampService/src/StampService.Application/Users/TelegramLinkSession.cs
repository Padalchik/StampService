namespace StampService.Application.Users;

public record TelegramLinkSession(
    Guid UserId,
    DateTime ExpiresAtUtc);

