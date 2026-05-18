using StampService.Application.Users;
using StampService.Domain.User;

namespace StampService.ApplicationTests.Fakes;

public class FakeUserRepository : IUserRepository
{
    private readonly Dictionary<(IdentityType Type, string Key), User> _usersByIdentity = [];
    private readonly Dictionary<string, User> _usersByCustomerCode = [];

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

    public Task<User?> GetByCustomerCodeAsync(string customerCode, CancellationToken cancellationToken)
    {
        _usersByCustomerCode.TryGetValue(customerCode, out var user);
        return Task.FromResult(user);
    }

    public Task<User?> GetByIdAsync(Guid userId, CancellationToken cancellationToken)
    {
        return Task.FromResult(Users.FirstOrDefault(user => user.Id == userId));
    }

    public Task<bool> CustomerCodeExistsAsync(string customerCode, CancellationToken cancellationToken)
    {
        return Task.FromResult(_usersByCustomerCode.ContainsKey(customerCode));
    }

    public void Add(User user)
    {
        Users.Add(user);
        _usersByCustomerCode[user.CustomerCode] = user;

        foreach (var identity in user.Identities)
            _usersByIdentity[(identity.Type, identity.Key)] = user;
    }

    public Task SaveAsync(CancellationToken cancellationToken)
    {
        SaveCount++;
        _usersByIdentity.Clear();
        foreach (var user in Users)
        {
            _usersByCustomerCode[user.CustomerCode] = user;
            foreach (var identity in user.Identities)
                _usersByIdentity[(identity.Type, identity.Key)] = user;
        }

        return Task.CompletedTask;
    }
}
