using StampService.Application.Abstractions;

namespace StampService.Application.Demo.Commands.CreateUserDemoData;

public record CreateUserDemoDataCommand(
    long AdminTelegramUserId,
    string CustomerCode,
    Guid BrandId) : ICommand;
