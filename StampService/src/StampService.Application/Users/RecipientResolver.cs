using FluentResults;
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
            return Result.Fail("Customer code must contain exactly 4 digits");

        var user = await _userRepository.GetByCustomerCodeAsync(customerCode, cancellationToken);
        if (user is null)
            return Result.Fail("Recipient not found");

        return Result.Ok(new RecipientResolutionResult(
            user.Id,
            user.Name,
            user.CustomerCode));
    }
}
