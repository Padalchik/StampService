using Microsoft.EntityFrameworkCore;
using Npgsql;
using StampService.Application.Errors;
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
            .ThenInclude(user => user.Identities)
            .FirstOrDefaultAsync(
                item => item.Type == identityType && item.Key == identityKey,
                cancellationToken);

        return identity?.User;
    }

    public async Task<User?> GetByIdAsync(Guid userId, CancellationToken cancellationToken)
    {
        return await _dbContext.Users
            .Include(user => user.Identities)
            .FirstOrDefaultAsync(user => user.Id == userId, cancellationToken);
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

    public async Task SaveIdentityAsync(
        User user,
        UserIdentity identity,
        CancellationToken cancellationToken)
    {
        var identityEntry = _dbContext.Entry(identity);
        if (identityEntry.State is EntityState.Detached or EntityState.Modified)
            _dbContext.UserIdentities.Add(identity);

        var userEntry = _dbContext.Entry(user);
        if (userEntry.State != EntityState.Added)
            userEntry.State = EntityState.Unchanged;

        try
        {
            await SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.UniqueViolation
        })
        {
            throw new ConcurrencyConflictException(
                "User identity changes conflicted with another operation.",
                ex);
        }
    }

    public Task SaveAsync(CancellationToken cancellationToken)
    {
        return SaveChangesAsync(cancellationToken);
    }

    private async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.UniqueViolation
        })
        {
            throw new ConcurrencyConflictException(
                "User changes conflicted with another operation.",
                ex);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            throw new ConcurrencyConflictException(
                "User changes were modified by another operation.",
                ex);
        }
    }
}
