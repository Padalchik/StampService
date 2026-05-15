using StampService.Application.CustomerNotifications;
using StampService.Domain.User;

namespace StampService.ApplicationTests.Fakes;

public class FakeCustomerDigestStateRepository : ICustomerDigestStateRepository
{
    private readonly Dictionary<Guid, CustomerDigestState> _states = [];

    public IReadOnlyCollection<CustomerDigestState> States => _states.Values;
    public int SaveCount { get; private set; }

    public Task<CustomerDigestState?> GetByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        _states.TryGetValue(userId, out var state);
        return Task.FromResult(state);
    }

    public Task<IReadOnlyCollection<Guid>> GetEligibleUserIdsAsync(
        DateTime nowUtc,
        TimeSpan interval,
        int take,
        CancellationToken cancellationToken)
    {
        IReadOnlyCollection<Guid> userIds = _states.Values
            .Where(state => state.CanSendDigest(nowUtc, interval))
            .Select(state => state.UserId)
            .Take(take)
            .ToArray();

        return Task.FromResult(userIds);
    }

    public void Add(CustomerDigestState state)
    {
        _states[state.UserId] = state;
    }

    public Task SaveAsync(CancellationToken cancellationToken)
    {
        SaveCount++;
        return Task.CompletedTask;
    }
}
