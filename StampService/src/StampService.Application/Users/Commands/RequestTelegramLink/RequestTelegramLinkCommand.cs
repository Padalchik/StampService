using StampService.Application.Abstractions;

namespace StampService.Application.Users.Commands.RequestTelegramLink;

public record RequestTelegramLinkCommand(Guid UserId) : ICommand;

