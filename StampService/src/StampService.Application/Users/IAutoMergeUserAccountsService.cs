using FluentResults;
using StampService.Domain.User;

namespace StampService.Application.Users;

public interface IAutoMergeUserAccountsService
{
    Task<Result> MergeSingleIdentitySourceIntoTargetAsync(
        User targetUser,
        User sourceUser,
        IdentityType identityType,
        string identityKey,
        DateTime nowUtc,
        CancellationToken cancellationToken);
}
