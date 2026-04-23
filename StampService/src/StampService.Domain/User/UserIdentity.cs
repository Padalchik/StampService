using FluentResults;
using StampService.Domain.Shared;

namespace StampService.Domain.User;

public class UserIdentity : BaseEntity
{
    public User User { get; private set; }
    public Guid UserId { get; private set; }
    public IdentityType Type { get; private set; }
    public string Key { get; private set; }
    public string Metadata { get; private set; }

    private UserIdentity(User user, IdentityType type, string key, string metadata)
    {
        User = user;
        UserId = user.Id;
        Type = type;
        Key = key;
        Metadata = metadata;
    }

    // EF Core
    protected UserIdentity()
    {
        User = null!;
        Key = null!;
        Metadata = null!;
    }

    public static Result<UserIdentity> Create(User user, IdentityType type, string key, string metadata)
    {
        if (type == IdentityType.None)
            return Result.Fail("Invalid identity type");

        if (string.IsNullOrWhiteSpace(key))
            return Result.Fail("Key cannot be empty");

        if (key.Length > Constants.MAX_IDENTITY_KEY_LENGTH)
            return Result.Fail($"Key must not exceed {Constants.MAX_IDENTITY_KEY_LENGTH} characters");

        if (string.IsNullOrWhiteSpace(metadata))
            return Result.Fail("Metadata cannot be empty");

        if (metadata.Length > Constants.MAX_IDENTITY_METADATA_LENGTH)
            return Result.Fail($"Metadata must not exceed {Constants.MAX_IDENTITY_METADATA_LENGTH} characters");

        var userIdentity = new UserIdentity(user, type, key, metadata);
        return Result.Ok(userIdentity);
    }
}
