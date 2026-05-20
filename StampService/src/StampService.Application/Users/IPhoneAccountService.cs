using FluentResults;
using StampService.Domain.User;

namespace StampService.Application.Users;

public interface IPhoneAccountService
{
    Task<Result<User>> GetOrCreateByPhoneAsync(
        string phoneNumber,
        DateTime verifiedAtUtc,
        CancellationToken cancellationToken);

    bool HasActivePhoneIdentity(User user);
}
