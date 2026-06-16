using FluentResults;
using StampService.Domain.User;

namespace StampService.Application.Users;

public interface IPhoneAccountService
{
    Task<Result<User>> GetOrCreateByPhoneAsync(
        string phoneNumber,
        DateTime verifiedAtUtc,
        CancellationToken cancellationToken);

    Task<Result<User>> GetOrCreateForBusinessOperationAsync(
        string phoneNumber,
        string? invalidField,
        CancellationToken cancellationToken);

    Task<Result<User>> GetExistingForBusinessOperationAsync(
        string phoneNumber,
        string? invalidField,
        CancellationToken cancellationToken);

    bool HasActivePhoneIdentity(User user);
}
