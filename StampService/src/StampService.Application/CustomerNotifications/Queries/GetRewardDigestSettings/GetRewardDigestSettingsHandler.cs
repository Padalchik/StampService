using FluentResults;
using StampService.Application.Abstractions;
using StampService.Application.Administration;
using StampService.Application.Errors;
using StampService.Contracts.DTOs.CustomerNotifications;

namespace StampService.Application.CustomerNotifications.Queries.GetRewardDigestSettings;

public class GetRewardDigestSettingsHandler
    : IQueryHandler<RewardDigestSettingsResponse, GetRewardDigestSettingsQuery>
{
    private readonly IAdminAccessService _adminAccessService;
    private readonly IRewardDigestSettingsRepository _settingsRepository;

    public GetRewardDigestSettingsHandler(
        IAdminAccessService adminAccessService,
        IRewardDigestSettingsRepository settingsRepository)
    {
        _adminAccessService = adminAccessService;
        _settingsRepository = settingsRepository;
    }

    public async Task<Result<RewardDigestSettingsResponse>> Handle(
        GetRewardDigestSettingsQuery query,
        CancellationToken cancellationToken)
    {
        if (!_adminAccessService.IsAdmin(query.AdminTelegramUserId))
            return Result.Fail(AccessErrors.AdminRequired());

        var settings = await _settingsRepository.GetOrCreateAsync(cancellationToken);
        return Result.Ok(settings.ToResponse());
    }
}
