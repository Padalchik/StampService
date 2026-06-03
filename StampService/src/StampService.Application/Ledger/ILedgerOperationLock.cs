using FluentResults;

namespace StampService.Application.Ledger;

public interface ILedgerOperationLock
{
    Task<Result<T>> ExecuteWithMetricBalanceLockAsync<T>(
        Guid userId,
        Guid brandId,
        Guid metricDefinitionId,
        Func<CancellationToken, Task<Result<T>>> operation,
        CancellationToken cancellationToken);

    Task<Result<T>> ExecuteWithCoinWalletLockAsync<T>(
        Guid userId,
        Guid brandId,
        Func<CancellationToken, Task<Result<T>>> operation,
        CancellationToken cancellationToken);
}
