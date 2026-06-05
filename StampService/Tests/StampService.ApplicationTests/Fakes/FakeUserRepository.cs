using StampService.Application.Users;
using StampService.Domain.User;

namespace StampService.ApplicationTests.Fakes;

public class FakeUserRepository : IUserRepository
{
    private readonly Dictionary<(IdentityType Type, string Key), User> _usersByIdentity = [];

    public List<User> Users { get; } = [];

    public int SaveCount { get; private set; }

    public Task<User?> GetByIdentityAsync(
        IdentityType identityType,
        string identityKey,
        CancellationToken cancellationToken)
    {
        _usersByIdentity.TryGetValue((identityType, identityKey), out var user);
        return Task.FromResult(user);
    }

    public Task<bool> ExistsAsync(Guid userId, CancellationToken cancellationToken)
    {
        return Task.FromResult(Users.Any(user => user.Id == userId));
    }

    public Task<User?> GetByIdAsync(Guid userId, CancellationToken cancellationToken)
    {
        return Task.FromResult(Users.FirstOrDefault(user => user.Id == userId));
    }

    public void Add(User user)
    {
        Users.Add(user);

        foreach (var identity in user.Identities.Where(identity => identity.DeletedAt is null))
            _usersByIdentity[(identity.Type, identity.Key)] = user;
    }

    public Task SaveAsync(CancellationToken cancellationToken)
    {
        SaveCount++;
        RebuildIndexes();

        return Task.CompletedTask;
    }

    public Task SaveIdentityAsync(
        User user,
        UserIdentity identity,
        CancellationToken cancellationToken)
    {
        if (!Users.Contains(user))
            Users.Add(user);

        SaveCount++;
        RebuildIndexes();

        return Task.CompletedTask;
    }

    private void RebuildIndexes()
    {
        _usersByIdentity.Clear();
        foreach (var user in Users)
        {
            foreach (var identity in user.Identities.Where(identity => identity.DeletedAt is null))
                _usersByIdentity[(identity.Type, identity.Key)] = user;
        }
    }
}
