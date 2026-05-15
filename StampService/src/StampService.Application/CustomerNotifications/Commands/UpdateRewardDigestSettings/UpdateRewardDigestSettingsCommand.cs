using StampService.Application.Abstractions;

namespace StampService.Application.CustomerNotifications.Commands.UpdateRewardDigestSettings;

public record UpdateRewardDigestSettingsCommand(
    long AdminTelegramUserId,
    bool Enabled,
    int MessageToUserIntervalMinutes,
    int ScanIntervalMinutes,
    int BatchSize,
    int MaxBrandsPerMessage,
    int MaxRewardsPerBrand) : ICommand;
