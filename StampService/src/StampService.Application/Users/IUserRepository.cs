using StampService.Domain.User;

namespace StampService.Application.Users;

public interface IUserRepository
{
    Task<User?> GetByIdentityAsync(
        IdentityType identityType,
        string identityKey,
        CancellationToken cancellationToken);

    Task<User?> GetByCustomerCodeAsync(string customerCode, CancellationToken cancellationToken);

    Task<User?> GetByIdAsync(Guid userId, CancellationToken cancellationToken);

    Task<bool> CustomerCodeExistsAsync(string customerCode, CancellationToken cancellationToken);

    Task<bool> ExistsAsync(Guid userId, CancellationToken cancellationToken);

    void Add(User user);

    Task SaveAsync(CancellationToken cancellationToken);
}
