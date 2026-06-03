using StampService.Application.Abstractions;

namespace StampService.Application.Users.Commands.RequestPhoneChangeCode;

public record RequestPhoneChangeCodeCommand(Guid UserId, string PhoneNumber) : ICommand;
