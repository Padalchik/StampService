using StampService.Domain.User;

namespace StampService.Application.CustomerNotifications;

public interface ICustomerDigestStateRepository
{
    Task<CustomerDigestState?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<Guid>> GetEligibleUserIdsAsync(
        DateTime nowUtc,
        TimeSpan interval,
        int take,
        CancellationToken cancellationToken);

    void Add(CustomerDigestState state);

    Task SaveAsync(CancellationToken cancellationToken);
}
