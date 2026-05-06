using StampService.Domain.Loyalty;

namespace StampService.Application.Metrics;

public interface IStampTransactionRepository
{
    Task<IReadOnlyCollection<StampTransaction>> GetHistoryByMetricBalanceAsync(
        Guid metricBalanceId,
        int skip,
        int take,
        CancellationToken cancellationToken);

    Task<int> CalculateMetricBalanceValueAsync(
        Guid metricBalanceId,
        CancellationToken cancellationToken);

    void Add(StampTransaction transaction);

    Task SaveAsync(CancellationToken cancellationToken);
}
