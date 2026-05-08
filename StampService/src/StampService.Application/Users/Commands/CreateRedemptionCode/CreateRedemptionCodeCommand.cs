using StampService.Application.Abstractions;

namespace StampService.Application.Users.Commands.CreateRedemptionCode;

public record CreateRedemptionCodeCommand(Guid UserId) : ICommand;
