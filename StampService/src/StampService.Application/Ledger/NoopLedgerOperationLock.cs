using FluentResults;

namespace StampService.Application.Ledger;

public sealed class NoopLedgerOperationLock : ILedgerOperationLock
{
    public static NoopLedgerOperationLock Instance { get; } = new();

    private NoopLedgerOperationLock()
    {
    }

    public Task<Result<T>> ExecuteWithMetricBalanceLockAsync<T>(
        Guid userId,
        Guid brandId,
        Guid metricDefinitionId,
        Func<CancellationToken, Task<Result<T>>> operation,
        CancellationToken cancellationToken)
    {
        return operation(cancellationToken);
    }

    public Task<Result<T>> ExecuteWithCoinWalletLockAsync<T>(
        Guid userId,
        Guid brandId,
        Func<CancellationToken, Task<Result<T>>> operation,
        CancellationToken cancellationToken)
    {
        return operation(cancellationToken);
    }
}
