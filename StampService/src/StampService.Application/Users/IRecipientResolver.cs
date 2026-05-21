using FluentResults;

namespace StampService.Application.Users;

public interface IRecipientResolver
{
    Task<Result<RecipientResolutionResult>> ResolveAsync(
        string publicIdentifier,
        CancellationToken cancellationToken);

    Task<Result<RecipientResolutionResult>> ResolveByPhoneAsync(
        string phoneNumber,
        CancellationToken cancellationToken);
}
