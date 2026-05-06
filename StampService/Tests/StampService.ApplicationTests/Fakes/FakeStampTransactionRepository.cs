using StampService.Application.Metrics;
using StampService.Domain.Loyalty;

namespace StampService.ApplicationTests.Fakes;

public class FakeStampTransactionRepository : IStampTransactionRepository
{
    private readonly List<StampTransaction> _transactions = [];

    public IReadOnlyCollection<StampTransaction> Transactions => _transactions;

    public Task<IReadOnlyCollection<StampTransaction>> GetHistoryByMetricBalanceAsync(
        Guid metricBalanceId,
        int skip,
        int take,
        CancellationToken cancellationToken)
    {
        IReadOnlyCollection<StampTransaction> result = _transactions
            .Where(transaction => transaction.MetricBalanceId == metricBalanceId)
            .OrderByDescending(transaction => transaction.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToArray();

        return Task.FromResult(result);
    }

    public Task<int> CalculateMetricBalanceValueAsync(
        Guid metricBalanceId,
        CancellationToken cancellationToken)
    {
        var value = _transactions
            .Where(transaction => transaction.MetricBalanceId == metricBalanceId)
            .Sum(transaction => transaction.Type == StampTransactionType.Issue
                ? transaction.Amount
                : -transaction.Amount);

        return Task.FromResult(value);
    }

    public void Add(StampTransaction transaction)
    {
        _transactions.Add(transaction);
    }

    public Task SaveAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
