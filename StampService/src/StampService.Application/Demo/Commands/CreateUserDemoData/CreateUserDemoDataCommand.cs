using StampService.Application.Abstractions;
using StampService.Application.Administration;

namespace StampService.Application.Demo.Commands.CreateUserDemoData;

public record CreateUserDemoDataCommand(
    AdminActor Admin,
    string PhoneNumber,
    Guid BrandId) : ICommand;
