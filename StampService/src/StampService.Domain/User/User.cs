using FluentResults;
using StampService.Domain.Shared;

namespace StampService.Domain.User;

public class User : BaseEntity
{
    private HashSet<UserIdentity> _userIdentities = [];

    public string Name { get; private set; }

    public IReadOnlyCollection<UserIdentity> Identities => _userIdentities;

    private User(string name)
    {
        Name = name;
    }

    // EF Core
    protected User()
    {
        Name = null!;
    }

    public static Result<User> Create(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Result.Fail("Name cannot be empty");

        var user = new User(name.Trim());
        return Result.Ok(user);
    }

    public Result<UserIdentity> AddIdentity(IdentityType type, string providerKey, string metadata)
    {
        var identityResult = UserIdentity.Create(this, type, providerKey, metadata);
        if (identityResult.IsFailed)
            return identityResult;

        var identity = identityResult.Value;

        var hasDuplicate = _userIdentities.Any(x => x.Type == identity.Type && x.Key == identity.Key);
        if (hasDuplicate)
            return Result.Fail("Identity already exists for this user");

        _userIdentities.Add(identity);
        Touch();

        return Result.Ok(identity);
    }
}
