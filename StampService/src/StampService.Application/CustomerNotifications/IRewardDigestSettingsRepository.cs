using StampService.Domain.CustomerNotifications;

namespace StampService.Application.CustomerNotifications;

public interface IRewardDigestSettingsRepository
{
    Task<RewardDigestSettings> GetOrCreateAsync(CancellationToken cancellationToken);

    Task SaveAsync(CancellationToken cancellationToken);
}
