using StampService.Application.Abstractions;

namespace StampService.Application.Users.Commands.UseRedemptionCode;

public record UseRedemptionCodeCommand(string RedemptionCode) : ICommand;
