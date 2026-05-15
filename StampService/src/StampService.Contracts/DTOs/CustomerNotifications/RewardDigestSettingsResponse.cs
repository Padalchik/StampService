namespace StampService.Contracts.DTOs.CustomerNotifications;

public record RewardDigestSettingsResponse(
    bool Enabled,
    int MessageToUserIntervalMinutes,
    int ScanIntervalMinutes,
    int BatchSize,
    int MaxBrandsPerMessage,
    int MaxRewardsPerBrand);
