using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using StampService.Application.CustomerNotifications;
using StampService.Domain.CustomerNotifications;

namespace StampService.Infrastructure.Repositories;

public class RewardDigestSettingsRepository : IRewardDigestSettingsRepository
{
    private readonly AppDbContext _dbContext;
    private readonly IOptions<RewardDigestOptions> _fallbackOptions;

    public RewardDigestSettingsRepository(
        AppDbContext dbContext,
        IOptions<RewardDigestOptions> fallbackOptions)
    {
        _dbContext = dbContext;
        _fallbackOptions = fallbackOptions;
    }

    public async Task<RewardDigestSettings> GetOrCreateAsync(CancellationToken cancellationToken)
    {
        var settings = await _dbContext.RewardDigestSettings
            .FirstOrDefaultAsync(item => item.Id == RewardDigestSettings.SingletonId, cancellationToken);
        if (settings is not null)
            return settings;

        var options = _fallbackOptions.Value;
        var createResult = RewardDigestSettings.Create(
            options.Enabled,
            options.MessageToUserIntervalMinutes,
            options.ScanIntervalMinutes,
            options.BatchSize,
            options.MaxBrandsPerMessage,
            options.MaxRewardsPerBrand);
        if (createResult.IsFailed)
            throw new InvalidOperationException("Reward digest fallback settings are invalid.");

        settings = createResult.Value;
        _dbContext.RewardDigestSettings.Add(settings);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return settings;
    }

    public Task SaveAsync(CancellationToken cancellationToken)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
}
