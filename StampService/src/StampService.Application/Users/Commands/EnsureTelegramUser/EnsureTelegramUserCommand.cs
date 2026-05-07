using StampService.Application.Abstractions;

namespace StampService.Application.Users.Commands.EnsureTelegramUser;

public record EnsureTelegramUserCommand(
    long TelegramUserId,
    string? FirstName,
    string? LastName,
    string? Username) : ICommand;
