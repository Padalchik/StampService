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
        if (string.IsNullOrWhiteSpace(key))
            return Result.Fail("Key не может быть пустым");

        if (key.Length > Constants.MAX_IDENTITY_KEY_LENGTH)
            return Result.Fail($"Key не должен превышать {Constants.MAX_IDENTITY_KEY_LENGTH} символов");

        if (string.IsNullOrWhiteSpace(metadata))
            return Result.Fail("Metadata не может быть пустым");

        if (metadata.Length > Constants.MAX_IDENTITY_METADATA_LENGTH)
            return Result.Fail($"Metadata не должен превышать {Constants.MAX_IDENTITY_METADATA_LENGTH} символов");

        var userIdentity = new UserIdentity(user, type, key, metadata);
        return Result.Ok(userIdentity);
    }
}