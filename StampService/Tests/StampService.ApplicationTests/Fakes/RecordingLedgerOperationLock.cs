using FluentResults;
using StampService.Application.Ledger;

namespace StampService.ApplicationTests.Fakes;

public class RecordingLedgerOperationLock : ILedgerOperationLock
{
    public List<(Guid UserId, Guid BrandId, Guid MetricDefinitionId)> MetricBalanceLocks { get; } = [];

    public List<(Guid UserId, Guid BrandId)> CoinWalletLocks { get; } = [];

    public Task<Result<T>> ExecuteWithMetricBalanceLockAsync<T>(
        Guid userId,
        Guid brandId,
        Guid metricDefinitionId,
        Func<CancellationToken, Task<Result<T>>> operation,
        CancellationToken cancellationToken)
    {
        MetricBalanceLocks.Add((userId, brandId, metricDefinitionId));
        return operation(cancellationToken);
    }

    public Task<Result<T>> ExecuteWithCoinWalletLockAsync<T>(
        Guid userId,
        Guid brandId,
        Func<CancellationToken, Task<Result<T>>> operation,
        CancellationToken cancellationToken)
    {
        CoinWalletLocks.Add((userId, brandId));
        return operation(cancellationToken);
    }
}
