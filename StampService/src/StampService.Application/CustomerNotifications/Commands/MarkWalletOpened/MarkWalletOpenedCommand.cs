using StampService.Application.Abstractions;

namespace StampService.Application.CustomerNotifications.Commands.MarkWalletOpened;

public record MarkWalletOpenedCommand(Guid UserId) : ICommand;
