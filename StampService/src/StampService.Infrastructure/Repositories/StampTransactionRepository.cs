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

    public void Add(StampTransaction transaction)
    {
        _dbContext.StampTransactions.Add(transaction);
    }

    public Task SaveAsync(CancellationToken cancellationToken)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
}
