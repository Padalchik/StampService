using StampService.Domain.User;

namespace StampService.Application.Users;

public interface IUserRepository
{
    Task<User?> GetByIdentityAsync(
        IdentityType identityType,
        string identityKey,
        CancellationToken cancellationToken);

    Task<bool> ExistsAsync(Guid userId, CancellationToken cancellationToken);

    void Add(User user);

    Task SaveAsync(CancellationToken cancellationToken);
}
