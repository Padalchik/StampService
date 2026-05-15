namespace StampService.Application.CustomerNotifications;

public class RewardDigestOptions
{
    public const string SectionName = "RewardDigest";

    public bool Enabled { get; init; } = true;
    public int MessageToUserIntervalMinutes { get; init; } = 10080;
    public int ScanIntervalMinutes { get; init; } = 60;
    public int BatchSize { get; init; } = 100;
    public int MaxBrandsPerMessage { get; init; } = 5;
    public int MaxRewardsPerBrand { get; init; } = 3;

    public TimeSpan Interval => TimeSpan.FromMinutes(Math.Max(1, MessageToUserIntervalMinutes));
    public TimeSpan ScanInterval => TimeSpan.FromMinutes(Math.Max(1, ScanIntervalMinutes));
}
