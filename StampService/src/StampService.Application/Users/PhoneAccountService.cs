using System.Text.Json;
using FluentResults;
using StampService.Domain.User;

namespace StampService.Application.Users;

public class PhoneAccountService : IPhoneAccountService
{
    private readonly IUserRepository _userRepository;
    private readonly ICustomerCodeGenerator _customerCodeGenerator;
    private readonly IUserDisplayNameGenerator _displayNameGenerator;

    public PhoneAccountService(
        IUserRepository userRepository,
        ICustomerCodeGenerator customerCodeGenerator,
        IUserDisplayNameGenerator displayNameGenerator)
    {
        _userRepository = userRepository;
        _customerCodeGenerator = customerCodeGenerator;
        _displayNameGenerator = displayNameGenerator;
    }

    public async Task<Result<User>> GetOrCreateByPhoneAsync(
        string phoneNumber,
        DateTime verifiedAtUtc,
        CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdentityAsync(
            IdentityType.Phone,
            phoneNumber,
            cancellationToken);
        if (user is not null)
            return Result.Ok(user);

        var customerCode = await _customerCodeGenerator.GenerateAsync(cancellationToken);
        var userResult = User.Create(_displayNameGenerator.Generate(), customerCode);
        if (userResult.IsFailed)
            return Result.Fail(userResult.Errors);

        user = userResult.Value;
        var metadata = JsonSerializer.Serialize(new
        {
            PhoneNumber = phoneNumber,
            VerifiedAtUtc = verifiedAtUtc
        });

        var identityResult = user.AddIdentity(IdentityType.Phone, phoneNumber, metadata);
        if (identityResult.IsFailed)
            return Result.Fail(identityResult.Errors);

        _userRepository.Add(user);
        return Result.Ok(user);
    }

    public bool HasActivePhoneIdentity(User user)
    {
        return user.HasActiveIdentity(IdentityType.Phone);
    }
}
