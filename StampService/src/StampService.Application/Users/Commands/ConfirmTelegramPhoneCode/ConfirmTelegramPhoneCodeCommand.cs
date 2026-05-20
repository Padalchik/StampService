using StampService.Application.Abstractions;

namespace StampService.Application.Users.Commands.ConfirmTelegramPhoneCode;

public record ConfirmTelegramPhoneCodeCommand(
    long TelegramUserId,
    string? FirstName,
    string? LastName,
    string? Username,
    string PhoneNumber,
    string Code) : ICommand;
