using StampService.Application.Abstractions;

namespace StampService.Application.Demo.Commands.ResetDemoDatabase;

public record ResetDemoDatabaseCommand(long AdminTelegramUserId) : ICommand;
