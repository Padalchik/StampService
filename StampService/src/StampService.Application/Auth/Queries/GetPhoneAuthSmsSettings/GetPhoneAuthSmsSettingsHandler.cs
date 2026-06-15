using FluentResults;
using StampService.Application.Abstractions;
using StampService.Application.Administration;
using StampService.Application.Errors;
using StampService.Contracts.DTOs.Auth;

namespace StampService.Application.Auth.Queries.GetPhoneAuthSmsSettings;

public class GetPhoneAuthSmsSettingsHandler
    : IQueryHandler<PhoneAuthSmsSettingsResponse, GetPhoneAuthSmsSettingsQuery>
{
    private readonly IAdminAccessService _adminAccessService;
    private readonly IPhoneAuthSmsSettingsRepository _settingsRepository;

    public GetPhoneAuthSmsSettingsHandler(
        IAdminAccessService adminAccessService,
        IPhoneAuthSmsSettingsRepository settingsRepository)
    {
        _adminAccessService = adminAccessService;
        _settingsRepository = settingsRepository;
    }

    public async Task<Result<PhoneAuthSmsSettingsResponse>> Handle(
        GetPhoneAuthSmsSettingsQuery query,
        CancellationToken cancellationToken)
    {
        if (!await _adminAccessService.IsAdminAsync(query.AdminActor, cancellationToken))
            return Result.Fail(AccessErrors.AdminRequired());

        var settings = await _settingsRepository.GetOrCreateAsync(cancellationToken);
        return Result.Ok(settings.ToResponse());
    }
}
