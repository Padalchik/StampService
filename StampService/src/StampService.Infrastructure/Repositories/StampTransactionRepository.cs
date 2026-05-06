using Microsoft.EntityFrameworkCore;
using StampService.Application.Metrics;
using StampService.Domain.Loyalty;

namespace StampService.Infrastructure.Repositories;

public class StampTransactionRepository : IStampTransactionRepository
{
    private readonly AppDbContext _dbContext;

    public StampTransactionRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyCollection<StampTransaction>> GetHistoryByMetricBalanceAsync(
        Guid metricBalanceId,
        int skip,
        int take,
        CancellationToken cancellationToken)
    {
        return await _dbContext.StampTransactions
            .AsNoTracking()
            .Where(transaction => transaction.MetricBalanceId == metricBalanceId)
            .OrderByDescending(transaction => transaction.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToArrayAsync(cancellationToken);
    }

    public void Add(StampTransaction transaction)
    {
        _dbContext.StampTransactions.Add(transaction);
    }

    public Task SaveAsync(CancellationToken cancellationToken)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
}
