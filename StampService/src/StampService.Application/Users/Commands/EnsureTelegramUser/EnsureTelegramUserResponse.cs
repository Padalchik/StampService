namespace StampService.Application.Users.Commands.EnsureTelegramUser;

public record EnsureTelegramUserResponse(
    Guid UserId,
    bool Created,
    string DisplayName);
