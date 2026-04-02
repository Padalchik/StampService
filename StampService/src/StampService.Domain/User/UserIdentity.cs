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
        Metadata = string.IsNullOrWhiteSpace(metadata) ? "{}" : metadata;
    }
        
    // EF Core  
    protected UserIdentity()
    {
    }

    public static Result<UserIdentity> Create(User user, IdentityType type, string key, string metadata)
    {
        if (string.IsNullOrWhiteSpace(key))
            return Result.Fail("Key не может быть пустым");

        var userIdentity = new UserIdentity(user, type, key, metadata);
        return Result.Ok(userIdentity);
    }
}