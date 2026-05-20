using StampService.Application.Abstractions;

namespace StampService.Application.Users.Commands.ConfirmTelegramLinkSession;

public record ConfirmTelegramLinkSessionCommand(
    string Token,
    long TelegramUserId,
    string? FirstName,
    string? LastName,
    string? Username) : ICommand;

