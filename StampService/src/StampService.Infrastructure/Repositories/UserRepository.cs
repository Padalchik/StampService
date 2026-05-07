using Microsoft.EntityFrameworkCore;
using StampService.Application.Users;
using StampService.Domain.User;

namespace StampService.Infrastructure.Repositories;

public class UserRepository : IUserRepository
{
    private readonly AppDbContext _dbContext;

    public UserRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<User?> GetByIdentityAsync(
        IdentityType identityType,
        string identityKey,
        CancellationToken cancellationToken)
    {
        var identity = await _dbContext.UserIdentities
            .Include(item => item.User)
            .FirstOrDefaultAsync(
                item => item.Type == identityType && item.Key == identityKey,
                cancellationToken);

        return identity?.User;
    }

    public async Task<User?> GetByCustomerCodeAsync(string customerCode, CancellationToken cancellationToken)
    {
        return await _dbContext.Users
            .FirstOrDefaultAsync(user => user.CustomerCode == customerCode, cancellationToken);
    }

    public async Task<bool> CustomerCodeExistsAsync(string customerCode, CancellationToken cancellationToken)
    {
        return await _dbContext.Users
            .AnyAsync(user => user.CustomerCode == customerCode, cancellationToken);
    }

    public async Task<bool> ExistsAsync(Guid userId, CancellationToken cancellationToken)
    {
        return await _dbContext.Users
            .AnyAsync(user => user.Id == userId, cancellationToken);
    }

    public void Add(User user)
    {
        _dbContext.Users.Add(user);
    }

    public Task SaveAsync(CancellationToken cancellationToken)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
}
