using StampService.Application.Abstractions;
using StampService.Application.Administration;

namespace StampService.Application.Demo.Commands.ResetDemoDatabase;

public record ResetDemoDatabaseCommand(AdminActor Admin) : ICommand;
