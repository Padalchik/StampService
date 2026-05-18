using FluentResults;
using System.Security.Cryptography;
using StampService.Domain.Shared;

namespace StampService.Domain.User;

public class User : BaseEntity
{
    public const int CustomerCodeLength = 4;

    private HashSet<UserIdentity> _identities = [];

    public string Name { get; private set; }

    public string CustomerCode { get; private set; }

    public IReadOnlyCollection<UserIdentity> Identities => _identities;

    private User(string name, string customerCode)
    {
        Name = name;
        CustomerCode = customerCode;
    }

    // EF Core
    protected User()
    {
        Name = null!;
        CustomerCode = null!;
    }

    public static Result<User> Create(string name)
    {
        return Create(name, GenerateCustomerCode());
    }

    public static Result<User> Create(string name, string customerCode)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Result.Fail(DomainError.Validation(
                "user.name_required",
                "Name cannot be empty",
                nameof(name)));

        if (!IsValidCustomerCode(customerCode))
            return Result.Fail(DomainError.Validation(
                "user.customer_code_invalid",
                "Customer code must contain exactly 4 digits",
                nameof(customerCode)));

        var user = new User(name.Trim(), customerCode);
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

    public static bool IsValidCustomerCode(string? customerCode)
    {
        return customerCode is not null
            && customerCode.Length == CustomerCodeLength
            && customerCode.All(char.IsDigit);
    }

    private static string GenerateCustomerCode()
    {
        var value = RandomNumberGenerator.GetInt32(0, 10_000);
        return value.ToString("D4");
    }
}
