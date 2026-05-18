using StampService.Application.Abstractions;

namespace StampService.Application.Users.Commands.RequestPhoneLinkCode;

public record RequestPhoneLinkCodeCommand(Guid UserId, string PhoneNumber) : ICommand;
