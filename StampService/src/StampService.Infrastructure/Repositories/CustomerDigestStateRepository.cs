using Microsoft.EntityFrameworkCore;
using StampService.Application.CustomerNotifications;
using StampService.Domain.User;

namespace StampService.Infrastructure.Repositories;

public class CustomerDigestStateRepository : ICustomerDigestStateRepository
{
    private readonly AppDbContext _dbContext;

    public CustomerDigestStateRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<CustomerDigestState?> GetByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        return _dbContext.CustomerDigestStates
            .FirstOrDefaultAsync(state => state.UserId == userId, cancellationToken);
    }

    public async Task<IReadOnlyCollection<Guid>> GetEligibleUserIdsAsync(
        DateTime nowUtc,
        TimeSpan interval,
        int take,
        CancellationToken cancellationToken)
    {
        var cutoff = nowUtc - interval;

        return await _dbContext.CustomerDigestStates
            .AsNoTracking()
            .Where(state => state.LastWalletOpenedAtUtc != null
                && state.LastWalletOpenedAtUtc <= cutoff
                && (state.LastDigestSentAtUtc == null || state.LastDigestSentAtUtc <= cutoff))
            .OrderBy(state => state.LastDigestSentAtUtc ?? DateTime.MinValue)
            .ThenBy(state => state.LastWalletOpenedAtUtc)
            .Select(state => state.UserId)
            .Take(take)
            .ToArrayAsync(cancellationToken);
    }

    public void Add(CustomerDigestState state)
    {
        _dbContext.CustomerDigestStates.Add(state);
    }

    public Task SaveAsync(CancellationToken cancellationToken)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
}
