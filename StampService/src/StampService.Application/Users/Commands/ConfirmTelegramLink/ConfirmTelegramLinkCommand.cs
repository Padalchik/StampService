using StampService.Application.Abstractions;
using StampService.Contracts.DTOs.Auth;

namespace StampService.Application.Users.Commands.ConfirmTelegramLink;

public record ConfirmTelegramLinkCommand(Guid UserId, TelegramLoginRequest TelegramLogin) : ICommand;
