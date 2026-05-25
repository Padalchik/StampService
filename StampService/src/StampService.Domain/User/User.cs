using FluentResults;
using StampService.Domain.Shared;

namespace StampService.Domain.User;

public class User : BaseEntity
{
    private HashSet<UserIdentity> _identities = [];

    public string Name { get; private set; }

    public IReadOnlyCollection<UserIdentity> Identities => _identities;

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
            return Result.Fail(DomainError.Validation(
                "user.name_required",
                "Name cannot be empty",
                nameof(name)));

        var user = new User(name.Trim());
        return Result.Ok(user);
    }

    public Result<UserIdentity> AddIdentity(IdentityType type, string providerKey, string metadata)
    {
        var identityResult = UserIdentity.Create(this, type, providerKey, metadata);
        if (identityResult.IsFailed)
            return identityResult;

        var identity = identityResult.Value;

        var hasDuplicate = _identities.Any(x =>
            x.DeletedAt is null
            && x.Type == identity.Type
            && x.Key == identity.Key);
        if (hasDuplicate)
            return Result.Fail(DomainError.Conflict(
                "user.identity_already_exists",
                "Identity already exists for this user"));

        _identities.Add(identity);
        Touch();

        return Result.Ok(identity);
    }

    public bool HasActiveIdentity(IdentityType type)
    {
        return _identities.Any(identity =>
            identity.DeletedAt is null && identity.Type == type);
    }

    public void Deactivate(DateTime deactivatedAtUtc)
    {
        if (DeletedAt is not null)
            return;

        ((ISoftDelete)this).DeletedAt = deactivatedAtUtc;
        Touch();
    }

}
