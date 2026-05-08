using StampService.Application.Coins;
using StampService.Domain.Coins;

namespace StampService.ApplicationTests.Fakes;

public class FakeCoinTransactionRepository : ICoinTransactionRepository
{
    private readonly List<CoinTransaction> _transactions = [];

    public IReadOnlyCollection<CoinTransaction> Transactions => _transactions;

    public int SaveCount { get; private set; }

    public Task<IReadOnlyCollection<CoinTransaction>> GetHistoryByWalletAsync(
        Guid coinWalletId,
        int skip,
        int take,
        CancellationToken cancellationToken)
    {
        IReadOnlyCollection<CoinTransaction> result = _transactions
            .Where(transaction => transaction.CoinWalletId == coinWalletId)
            .OrderByDescending(transaction => transaction.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToArray();

        return Task.FromResult(result);
    }

    public Task<int> CalculateWalletValueAsync(
        Guid coinWalletId,
        CancellationToken cancellationToken)
    {
        var value = _transactions
            .Where(transaction => transaction.CoinWalletId == coinWalletId)
            .Sum(transaction => transaction.Type == CoinTransactionType.Issue
                ? transaction.Amount
                : -transaction.Amount);

        return Task.FromResult(value);
    }

    public void Add(CoinTransaction transaction)
    {
        _transactions.Add(transaction);
    }

    public Task SaveAsync(CancellationToken cancellationToken)
    {
        SaveCount++;
        return Task.CompletedTask;
    }
}
