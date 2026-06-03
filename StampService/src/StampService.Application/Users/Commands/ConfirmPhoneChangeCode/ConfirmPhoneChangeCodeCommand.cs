using StampService.Application.Abstractions;

namespace StampService.Application.Users.Commands.ConfirmPhoneChangeCode;

public record ConfirmPhoneChangeCodeCommand(
    Guid UserId,
    string PhoneNumber,
    string Code,
    Guid? AuthCodeId = null) : ICommand;
