using FluentResults;
using StampService.Application.Auth;
using StampService.Application.Errors;
using StampService.Domain.User;

namespace StampService.Application.Users;

public class RecipientResolver : IRecipientResolver
{
    private readonly IUserRepository _userRepository;

    public RecipientResolver(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<Result<RecipientResolutionResult>> ResolveAsync(
        string publicIdentifier,
        CancellationToken cancellationToken)
    {
        var customerCode = publicIdentifier.Trim();
        if (!User.IsValidCustomerCode(customerCode))
            return Result.Fail(UserErrors.CustomerCodeInvalid());

        var user = await _userRepository.GetByCustomerCodeAsync(customerCode, cancellationToken);
        if (user is null)
            return Result.Fail(UserErrors.RecipientNotFound());

        return Result.Ok(new RecipientResolutionResult(
            user.Id,
            user.Name,
            user.CustomerCode));
    }

    public async Task<Result<RecipientResolutionResult>> ResolveByPhoneAsync(
        string phoneNumber,
        CancellationToken cancellationToken)
    {
        var normalizedPhoneResult = PhoneNumberNormalizer.NormalizeForAuth(phoneNumber, "phoneNumber");
        if (normalizedPhoneResult.IsFailed)
            return Result.Fail(normalizedPhoneResult.Errors);

        var user = await _userRepository.GetByIdentityAsync(
            IdentityType.Phone,
            normalizedPhoneResult.Value,
            cancellationToken);

        if (user is null)
            return Result.Fail(UserErrors.RecipientNotFound());

        return Result.Ok(new RecipientResolutionResult(
            user.Id,
            user.Name,
            user.CustomerCode));
    }
}
