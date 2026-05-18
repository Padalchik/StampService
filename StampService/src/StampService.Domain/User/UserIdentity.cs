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
        var validationResult = Validate(type, key, metadata);
        if (validationResult.IsFailed)
            return Result.Fail(validationResult.Errors);

        var userIdentity = new UserIdentity(user, type, key, metadata);
        return Result.Ok(userIdentity);
    }

    public void Deactivate(DateTime deactivatedAtUtc)
    {
        if (DeletedAt is not null)
            return;

        ((ISoftDelete)this).DeletedAt = deactivatedAtUtc;
        Touch();
    }

    private static Result Validate(IdentityType type, string key, string metadata)
    {
        if (type == IdentityType.None)
            return Result.Fail(DomainError.Validation(
                "user_identity.type_invalid",
                "Invalid identity type",
                nameof(type)));

        if (string.IsNullOrWhiteSpace(key))
            return Result.Fail(DomainError.Validation(
                "user_identity.key_required",
                "Key cannot be empty",
                nameof(key)));

        if (key.Length > Constants.MAX_IDENTITY_KEY_LENGTH)
            return Result.Fail(DomainError.Validation(
                "user_identity.key_too_long",
                $"Key must not exceed {Constants.MAX_IDENTITY_KEY_LENGTH} characters",
                nameof(key)));

        if (string.IsNullOrWhiteSpace(metadata))
            return Result.Fail(DomainError.Validation(
                "user_identity.metadata_required",
                "Metadata cannot be empty",
                nameof(metadata)));

        if (metadata.Length > Constants.MAX_IDENTITY_METADATA_LENGTH)
            return Result.Fail(DomainError.Validation(
                "user_identity.metadata_too_long",
                $"Metadata must not exceed {Constants.MAX_IDENTITY_METADATA_LENGTH} characters",
                nameof(metadata)));

        return Result.Ok();
    }
}
