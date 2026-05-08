using Microsoft.EntityFrameworkCore;
using StampService.Application.Coins;
using StampService.Domain.Coins;

namespace StampService.Infrastructure.Repositories;

public class CoinTransactionRepository : ICoinTransactionRepository
{
    private readonly AppDbContext _dbContext;

    public CoinTransactionRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyCollection<CoinTransaction>> GetHistoryByWalletAsync(
        Guid coinWalletId,
        int skip,
        int take,
        CancellationToken cancellationToken)
    {
        return await _dbContext.CoinTransactions
            .AsNoTracking()
            .Where(transaction => transaction.CoinWalletId == coinWalletId)
            .OrderByDescending(transaction => transaction.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToArrayAsync(cancellationToken);
    }

    public Task<int> CalculateWalletValueAsync(
        Guid coinWalletId,
        CancellationToken cancellationToken)
    {
        return _dbContext.CoinTransactions
            .Where(transaction => transaction.CoinWalletId == coinWalletId)
            .SumAsync(
                transaction => transaction.Type == CoinTransactionType.Issue
                    ? transaction.Amount
                    : -transaction.Amount,
                cancellationToken);
    }

    public void Add(CoinTransaction transaction)
    {
        _dbContext.CoinTransactions.Add(transaction);
    }

    public Task SaveAsync(CancellationToken cancellationToken)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
}
