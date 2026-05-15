using FluentResults;
using StampService.Application.Abstractions;
using StampService.Application.Administration;
using StampService.Application.Errors;
using StampService.Contracts.DTOs.CustomerNotifications;

namespace StampService.Application.CustomerNotifications.Commands.UpdateRewardDigestSettings;

public class UpdateRewardDigestSettingsHandler
    : ICommandHandler<RewardDigestSettingsResponse, UpdateRewardDigestSettingsCommand>
{
    private readonly IAdminAccessService _adminAccessService;
    private readonly IRewardDigestSettingsRepository _settingsRepository;

    public UpdateRewardDigestSettingsHandler(
        IAdminAccessService adminAccessService,
        IRewardDigestSettingsRepository settingsRepository)
    {
        _adminAccessService = adminAccessService;
        _settingsRepository = settingsRepository;
    }

    public async Task<Result<RewardDigestSettingsResponse>> Handle(
        UpdateRewardDigestSettingsCommand command,
        CancellationToken cancellationToken)
    {
        if (!_adminAccessService.IsAdmin(command.AdminTelegramUserId))
            return Result.Fail(AccessErrors.AdminRequired());

        var settings = await _settingsRepository.GetOrCreateAsync(cancellationToken);
        var updateResult = settings.Update(
            command.Enabled,
            command.MessageToUserIntervalMinutes,
            command.ScanIntervalMinutes,
            command.BatchSize,
            command.MaxBrandsPerMessage,
            command.MaxRewardsPerBrand);
        if (updateResult.IsFailed)
            return Result.Fail(updateResult.Errors);

        await _settingsRepository.SaveAsync(cancellationToken);
        return Result.Ok(settings.ToResponse());
    }
}
