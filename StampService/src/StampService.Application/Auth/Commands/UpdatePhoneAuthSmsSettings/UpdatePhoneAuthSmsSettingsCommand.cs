using StampService.Application.Abstractions;
using StampService.Application.Administration;

namespace StampService.Application.Auth.Commands.UpdatePhoneAuthSmsSettings;

public record UpdatePhoneAuthSmsSettingsCommand(
    AdminActor AdminActor,
    bool IsEnabled) : ICommand;
