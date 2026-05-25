using StampService.Application.Abstractions;

namespace StampService.Application.Demo.Commands.CreateUserDemoData;

public record CreateUserDemoDataCommand(
    long AdminTelegramUserId,
    string PhoneNumber,
    Guid BrandId) : ICommand;
