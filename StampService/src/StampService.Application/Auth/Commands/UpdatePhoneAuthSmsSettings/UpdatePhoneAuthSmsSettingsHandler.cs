using FluentResults;
using StampService.Application.Abstractions;
using StampService.Application.Administration;
using StampService.Application.Errors;
using StampService.Contracts.DTOs.Auth;

namespace StampService.Application.Auth.Commands.UpdatePhoneAuthSmsSettings;

public class UpdatePhoneAuthSmsSettingsHandler
    : ICommandHandler<PhoneAuthSmsSettingsResponse, UpdatePhoneAuthSmsSettingsCommand>
{
    private readonly IAdminAccessService _adminAccessService;
    private readonly IPhoneAuthSmsSettingsRepository _settingsRepository;

    public UpdatePhoneAuthSmsSettingsHandler(
        IAdminAccessService adminAccessService,
        IPhoneAuthSmsSettingsRepository settingsRepository)
    {
        _adminAccessService = adminAccessService;
        _settingsRepository = settingsRepository;
    }

    public async Task<Result<PhoneAuthSmsSettingsResponse>> Handle(
        UpdatePhoneAuthSmsSettingsCommand command,
        CancellationToken cancellationToken)
    {
        if (!await _adminAccessService.IsAdminAsync(command.AdminActor, cancellationToken))
            return Result.Fail(AccessErrors.AdminRequired());

        var settings = await _settingsRepository.GetOrCreateAsync(cancellationToken);
        settings.Update(command.IsEnabled);
        await _settingsRepository.SaveAsync(cancellationToken);

        return Result.Ok(settings.ToResponse());
    }
}
