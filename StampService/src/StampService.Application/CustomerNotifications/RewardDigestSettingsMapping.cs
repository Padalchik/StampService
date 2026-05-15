using StampService.Contracts.DTOs.CustomerNotifications;
using StampService.Domain.CustomerNotifications;

namespace StampService.Application.CustomerNotifications;

public static class RewardDigestSettingsMapping
{
    public static RewardDigestSettingsResponse ToResponse(this RewardDigestSettings settings)
    {
        return new RewardDigestSettingsResponse(
            settings.Enabled,
            settings.MessageToUserIntervalMinutes,
            settings.ScanIntervalMinutes,
            settings.BatchSize,
            settings.MaxBrandsPerMessage,
            settings.MaxRewardsPerBrand);
    }
}
