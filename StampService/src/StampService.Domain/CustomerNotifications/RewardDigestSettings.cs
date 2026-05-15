using FluentResults;
using StampService.Domain.Shared;

namespace StampService.Domain.CustomerNotifications;

public class RewardDigestSettings
{
    public const int SingletonId = 1;

    public int Id { get; private set; }
    public bool Enabled { get; private set; }
    public int MessageToUserIntervalMinutes { get; private set; }
    public int ScanIntervalMinutes { get; private set; }
    public int BatchSize { get; private set; }
    public int MaxBrandsPerMessage { get; private set; }
    public int MaxRewardsPerBrand { get; private set; }

    private RewardDigestSettings(
        bool enabled,
        int messageToUserIntervalMinutes,
        int scanIntervalMinutes,
        int batchSize,
        int maxBrandsPerMessage,
        int maxRewardsPerBrand)
    {
        Id = SingletonId;
        Enabled = enabled;
        MessageToUserIntervalMinutes = messageToUserIntervalMinutes;
        ScanIntervalMinutes = scanIntervalMinutes;
        BatchSize = batchSize;
        MaxBrandsPerMessage = maxBrandsPerMessage;
        MaxRewardsPerBrand = maxRewardsPerBrand;
    }

    protected RewardDigestSettings()
    {
    }

    public static Result<RewardDigestSettings> Create(
        bool enabled,
        int messageToUserIntervalMinutes,
        int scanIntervalMinutes,
        int batchSize,
        int maxBrandsPerMessage,
        int maxRewardsPerBrand)
    {
        var validation = Validate(
            messageToUserIntervalMinutes,
            scanIntervalMinutes,
            batchSize,
            maxBrandsPerMessage,
            maxRewardsPerBrand);
        if (validation.IsFailed)
            return Result.Fail(validation.Errors);

        return Result.Ok(new RewardDigestSettings(
            enabled,
            messageToUserIntervalMinutes,
            scanIntervalMinutes,
            batchSize,
            maxBrandsPerMessage,
            maxRewardsPerBrand));
    }

    public Result Update(
        bool enabled,
        int messageToUserIntervalMinutes,
        int scanIntervalMinutes,
        int batchSize,
        int maxBrandsPerMessage,
        int maxRewardsPerBrand)
    {
        var validation = Validate(
            messageToUserIntervalMinutes,
            scanIntervalMinutes,
            batchSize,
            maxBrandsPerMessage,
            maxRewardsPerBrand);
        if (validation.IsFailed)
            return Result.Fail(validation.Errors);

        Enabled = enabled;
        MessageToUserIntervalMinutes = messageToUserIntervalMinutes;
        ScanIntervalMinutes = scanIntervalMinutes;
        BatchSize = batchSize;
        MaxBrandsPerMessage = maxBrandsPerMessage;
        MaxRewardsPerBrand = maxRewardsPerBrand;

        return Result.Ok();
    }

    private static Result Validate(
        int messageToUserIntervalMinutes,
        int scanIntervalMinutes,
        int batchSize,
        int maxBrandsPerMessage,
        int maxRewardsPerBrand)
    {
        if (messageToUserIntervalMinutes <= 0)
            return Result.Fail(DomainError.Validation(
                "reward_digest.message_interval_invalid",
                "Message interval must be positive",
                nameof(messageToUserIntervalMinutes)));

        if (scanIntervalMinutes <= 0)
            return Result.Fail(DomainError.Validation(
                "reward_digest.scan_interval_invalid",
                "Scan interval must be positive",
                nameof(scanIntervalMinutes)));

        if (batchSize <= 0)
            return Result.Fail(DomainError.Validation(
                "reward_digest.batch_size_invalid",
                "Batch size must be positive",
                nameof(batchSize)));

        if (maxBrandsPerMessage <= 0)
            return Result.Fail(DomainError.Validation(
                "reward_digest.max_brands_invalid",
                "Max brands per message must be positive",
                nameof(maxBrandsPerMessage)));

        if (maxRewardsPerBrand <= 0)
            return Result.Fail(DomainError.Validation(
                "reward_digest.max_rewards_invalid",
                "Max rewards per brand must be positive",
                nameof(maxRewardsPerBrand)));

        return Result.Ok();
    }
}
