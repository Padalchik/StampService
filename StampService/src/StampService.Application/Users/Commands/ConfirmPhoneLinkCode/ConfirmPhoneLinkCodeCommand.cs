using StampService.Application.Abstractions;

namespace StampService.Application.Users.Commands.ConfirmPhoneLinkCode;

public record ConfirmPhoneLinkCodeCommand(
    Guid UserId,
    string PhoneNumber,
    string Code,
    Guid? AuthCodeId = null) : ICommand;
