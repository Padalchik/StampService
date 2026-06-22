using System.Text.Json;
using FluentResults;
using StampService.Application.Auth;
using StampService.Application.Errors;
using StampService.Domain.User;

namespace StampService.Application.Users;

public class PhoneAccountService : IPhoneAccountService
{
    private readonly IUserRepository _userRepository;
    private readonly IUserDisplayNameGenerator _displayNameGenerator;

    public PhoneAccountService(
        IUserRepository userRepository,
        IUserDisplayNameGenerator displayNameGenerator)
    {
        _userRepository = userRepository;
        _displayNameGenerator = displayNameGenerator;
    }

    public async Task<Result<User>> GetOrCreateByPhoneAsync(
        string phoneNumber,
        DateTime verifiedAtUtc,
        CancellationToken cancellationToken)
    {
        var metadata = JsonSerializer.Serialize(new
        {
            PhoneNumber = phoneNumber,
            VerifiedAtUtc = verifiedAtUtc
        });

        var accountResult = await GetOrCreateByNormalizedPhoneAsync(
            phoneNumber,
            metadata,
            cancellationToken);

        return accountResult.IsFailed
            ? Result.Fail<User>(accountResult.Errors)
            : Result.Ok(accountResult.Value.User);
    }

    public async Task<Result<User>> GetOrCreateForBusinessOperationAsync(
        string phoneNumber,
        string? invalidField,
        CancellationToken cancellationToken)
    {
        var accountResult = await GetOrCreateForBusinessOperationWithStatusAsync(
            phoneNumber,
            invalidField,
            cancellationToken);

        return accountResult.IsFailed
            ? Result.Fail<User>(accountResult.Errors)
            : Result.Ok(accountResult.Value.User);
    }

    public async Task<Result<PhoneAccountOperationResult>> GetOrCreateForBusinessOperationWithStatusAsync(
        string phoneNumber,
        string? invalidField,
        CancellationToken cancellationToken)
    {
        var phoneNumberResult = PhoneNumberNormalizer.NormalizeForAuth(phoneNumber, invalidField);
        if (phoneNumberResult.IsFailed)
            return Result.Fail<PhoneAccountOperationResult>(phoneNumberResult.Errors);

        var normalizedPhoneNumber = phoneNumberResult.Value;
        var metadata = JsonSerializer.Serialize(new
        {
            PhoneNumber = normalizedPhoneNumber
        });

        var accountResult = await GetOrCreateByNormalizedPhoneAsync(
            normalizedPhoneNumber,
            metadata,
            cancellationToken);
        if (accountResult.IsFailed)
            return Result.Fail<PhoneAccountOperationResult>(accountResult.Errors);

        return Result.Ok(new PhoneAccountOperationResult(
            accountResult.Value.User,
            accountResult.Value.Created));
    }

    public async Task<Result<User>> GetExistingForBusinessOperationAsync(
        string phoneNumber,
        string? invalidField,
        CancellationToken cancellationToken)
    {
        var phoneNumberResult = PhoneNumberNormalizer.NormalizeForAuth(phoneNumber, invalidField);
        if (phoneNumberResult.IsFailed)
            return Result.Fail<User>(phoneNumberResult.Errors);

        var user = await _userRepository.GetByIdentityAsync(
            IdentityType.Phone,
            phoneNumberResult.Value,
            cancellationToken);

        return user is null
            ? Result.Fail<User>(UserErrors.RecipientNotFound())
            : Result.Ok(user);
    }

    public bool HasActivePhoneIdentity(User user)
    {
        return user.HasActiveIdentity(IdentityType.Phone);
    }

    private async Task<Result<PhoneAccountLookupResult>> GetOrCreateByNormalizedPhoneAsync(
        string phoneNumber,
        string identityMetadata,
        CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdentityAsync(
            IdentityType.Phone,
            phoneNumber,
            cancellationToken);
        if (user is not null)
            return Result.Ok(new PhoneAccountLookupResult(user, Created: false));

        var userResult = User.Create(_displayNameGenerator.Generate());
        if (userResult.IsFailed)
            return Result.Fail<PhoneAccountLookupResult>(userResult.Errors);

        user = userResult.Value;

        var identityResult = user.AddIdentity(IdentityType.Phone, phoneNumber, identityMetadata);
        if (identityResult.IsFailed)
            return Result.Fail<PhoneAccountLookupResult>(identityResult.Errors);

        _userRepository.Add(user);
        return Result.Ok(new PhoneAccountLookupResult(user, Created: true));
    }

    private sealed record PhoneAccountLookupResult(User User, bool Created);
}
